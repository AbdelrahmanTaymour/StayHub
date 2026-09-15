using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;
using StayHub.Infrastructure.Authentication.Models;

namespace StayHub.Infrastructure.Authentication;

internal sealed class AuthenticationService(
    HttpClient httpClient,
    IOptions<KeycloakOptions> keycloakOptions) : IAuthenticationService
{
    private static readonly TimeSpan ForgotPasswordTimeout = TimeSpan.FromSeconds(10);
    private readonly KeycloakOptions _keycloakOptions = keycloakOptions.Value;

    public async Task<Result<string>> RegisterAsync(
        User user,
        string password,
        CancellationToken cancellationToken = default)
    {
        var userRepresentation = new UserRepresentationModel
        {
            Username = user.Email,
            Email = user.Email,
            FirstName = user.FirstName.Value,
            LastName = user.LastName.Value,
            Enabled = true,
            EmailVerified = true,
            Credentials =
            [
                new CredentialRepresentationModel
                {
                    Type = "password",
                    Value = password,
                    Temporary = false
                }
            ]
        };

        HttpResponseMessage response;

        try
        {
            // NOTE: AdminAuthorizationDelegatingHandler calls EnsureSuccessStatusCode() internally,
            // so a non-2xx response surfaces here as an HttpRequestException, not as this response
            // object - see the catch block below, which recovers the status code from the exception.
            response = await httpClient.PostAsJsonAsync(
                "users",
                userRepresentation,
                cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            return Result.Failure<string>(UserErrors.EmailNotUnique);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<string>(AuthenticationErrors.RegistrationFailed);
        }

        // Keycloak returns the new user's location as .../admin/realms/StayHub/users/{id} - the
        // identity id is the last path segment.
        var identityId = response.Headers.Location?
            .Segments[^1]
            .TrimEnd('/');

        if (identityId is null) return Result.Failure<string>(AuthenticationErrors.RegistrationFailed);

        return identityId;
    }

    public async Task<Result> ForgotPasswordAsync(
        string identityId,
        CancellationToken cancellationToken = default)
    {
        var query =
            $"client_id={Uri.EscapeDataString(_keycloakOptions.PasswordResetClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(_keycloakOptions.PasswordResetRedirectUri)}" +
            $"&lifespan={_keycloakOptions.PasswordResetLinkLifespanSeconds}";

        using var timeoutCts = new CancellationTokenSource(ForgotPasswordTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"users/{identityId}/execute-actions-email?{query}",
                new[] { "UPDATE_PASSWORD" },
                linkedCts.Token);

            response.EnsureSuccessStatusCode();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(AuthenticationErrors.IdentityProviderUnavailable);
        }
        catch (HttpRequestException)
        {
            return Result.Failure(AuthenticationErrors.IdentityProviderUnavailable);
        }

        return Result.Success();
    }
}