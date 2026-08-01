using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;
using StayHub.Infrastructure.Authentication.Models;

namespace StayHub.Infrastructure.Authentication;

internal sealed class JwtService(HttpClient httpClient, IOptions<KeycloakOptions> keycloakOptions) : IJwtService
{
    private readonly KeycloakOptions _keycloakOptions = keycloakOptions.Value;

    public async Task<Result<AccessTokenResponse>> GetAccessTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var parameters = new KeyValuePair<string, string>[]
        {
            new("client_id", _keycloakOptions.AuthClientId),
            new("client_secret", _keycloakOptions.AuthClientSecret),
            new("grant_type", "password"),
            new("username", email),
            new("password", password),
            new("scope", "openid email")
        };

        return await RequestTokenAsync(parameters, cancellationToken);
    }

    public async Task<Result<AccessTokenResponse>> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var parameters = new KeyValuePair<string, string>[]
        {
            new("client_id", _keycloakOptions.AuthClientId),
            new("client_secret", _keycloakOptions.AuthClientSecret),
            new("grant_type", "refresh_token"),
            new("refresh_token", refreshToken)
        };

        return await RequestTokenAsync(parameters, cancellationToken);
    }

    public async Task<Result> LogOutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var parameters = new KeyValuePair<string, string>[]
        {
            new("client_id", _keycloakOptions.AuthClientId),
            new("client_secret", _keycloakOptions.AuthClientSecret),
            new("refresh_token", refreshToken)
        };

        using var content = new FormUrlEncodedContent(parameters);

        HttpResponseMessage response;

        try
        {
            // Relative URI resolution swaps the last segment of the token endpoint ("token") for
            // "logout", landing on .../protocol/openid-connect/logout - no separate base URL needed.
            response = await httpClient.PostAsync("logout", content, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result.Failure(AuthenticationErrors.IdentityProviderUnavailable);
        }

        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(AuthenticationErrors.IdentityProviderUnavailable);
    }

    private async Task<Result<AccessTokenResponse>> RequestTokenAsync(
        KeyValuePair<string, string>[] parameters,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(parameters);

        HttpResponseMessage response;

        try
        {
            // HttpClient.BaseAddress is the full token endpoint URL, so this posts directly to it.
            response = await httpClient.PostAsync(string.Empty, content, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.IdentityProviderUnavailable);
        }

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            // Covers wrong password, expired/revoked refresh token, or a non-existent user alike -
            // deliberately not distinguished further, to avoid leaking which emails are registered.
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.InvalidCredentials);

        if (!response.IsSuccessStatusCode)
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.IdentityProviderUnavailable);

        var token = await response.Content.ReadFromJsonAsync<AuthorizationToken>(cancellationToken);

        if (token is null) return Result.Failure<AccessTokenResponse>(AuthenticationErrors.IdentityProviderUnavailable);

        return new AccessTokenResponse(token.AccessToken, token.RefreshToken, token.ExpiresIn);
    }
}