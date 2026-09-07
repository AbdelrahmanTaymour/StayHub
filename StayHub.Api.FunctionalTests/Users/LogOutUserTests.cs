using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.Endpoints.Users;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Abstractions.Authentication;

namespace StayHub.Api.FunctionalTests.Users;

public class LogOutUserTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task LogOut_ShouldReturnNoContent_WhenAuthenticatedAndTokenIsValid()
    {
        // Arrange
        var (_, request, _) = await RegisterAndAuthenticateAsync();

        var loginResponse = await HttpClient.PostAsJsonAsync(
            "api/v1/users/login",
            new LogInUserRequest(request.Email, request.Password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();

        AuthenticateAs(loginBody!.AccessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/users/logout",
            new LogOutUserRequest(loginBody.RefreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LogOut_ShouldReturnUnauthorized_WhenNoTokenIsProvided()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/users/logout",
            new LogOutUserRequest("some-refresh-token"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogOut_ShouldReturnBadRequest_WhenRefreshTokenIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/users/logout",
            new LogOutUserRequest(string.Empty));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}