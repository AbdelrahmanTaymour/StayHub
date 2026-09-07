using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Users.GetUser;

namespace StayHub.Api.FunctionalTests.Users;

public class GetLoggedInUserTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetLoggedInUser_ShouldReturnOwnDetails_WhenAuthenticated()
    {
        // Arrange
        var (accessToken, request, userId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync("api/v1/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body!.Id.Should().Be(userId);
        body.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task GetLoggedInUser_ShouldReturnUnauthorized_WhenNoTokenIsProvided()
    {
        // Act
        var response = await HttpClient.GetAsync("api/v1/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}