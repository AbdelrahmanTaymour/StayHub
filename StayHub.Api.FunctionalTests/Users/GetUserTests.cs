using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Users.GetUser;

namespace StayHub.Api.FunctionalTests.Users;

public class GetUserTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetUser_ShouldReturnOwnDetails_WhenCallerIsTheUser()
    {
        // Arrange
        var (accessToken, request, userId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync($"api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body!.Id.Should().Be(userId);
        body.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task GetUser_ShouldReturnForbidden_WhenCallerIsAnUnrelatedUser()
    {
        // Arrange
        var (_, _, targetUserId) = await RegisterAndAuthenticateAsync();
        var (callerAccessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(callerAccessToken);

        // Act
        var response = await HttpClient.GetAsync($"api/v1/users/{targetUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUser_ShouldReturnUnauthorized_WhenNoTokenIsProvided()
    {
        // Arrange
        var (_, _, userId) = await RegisterAndAuthenticateAsync();
        // Deliberately not calling AuthenticateAs — no bearer token attached.

        // Act
        var response = await HttpClient.GetAsync($"api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUser_ShouldReturnForbidden_WhenTargetUserDoesNotExistAndCallerIsNotAdmin()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync($"api/v1/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUser_ShouldReturnNotFound_WhenTargetUserDoesNotExistAndCallerIsAdmin()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(userId);
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync($"api/v1/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUser_ShouldReturnNotFound_WhenRouteIdIsMalformed()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync("api/v1/users/not-a-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}