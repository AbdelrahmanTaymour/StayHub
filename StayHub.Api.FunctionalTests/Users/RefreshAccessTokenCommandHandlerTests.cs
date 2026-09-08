using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.Endpoints.Users;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Abstractions.Authentication;

namespace StayHub.Api.FunctionalTests.Users;

public class RefreshAccessTokenCommandHandlerTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task RefreshToken_ShouldReturnNewAccessToken_WhenRefreshTokenIsValid()
    {
        // Arrange
        var (_, request, _) = await RegisterAndAuthenticateAsync();

        var loginResponse = await HttpClient.PostAsJsonAsync(
            "api/v1/users/login",
            new LogInUserRequest(request.Email, request.Password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/users/refresh-token",
            new RefreshAccessTokenRequest(loginBody!.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenRefreshTokenIsInvalid()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/users/refresh-token",
            new RefreshAccessTokenRequest("completely-invalid-refresh-token"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnBadRequest_WhenTokenIsEmpty()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/users/refresh-token",
            new RefreshAccessTokenRequest(string.Empty));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}