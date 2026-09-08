using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.Endpoints.Users;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Users.GetUser;

namespace StayHub.Api.FunctionalTests.Users;

public class UpdateUserNameCommandHandlerTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task UpdateName_ShouldReturnNoContent_WhenCallerIsTheUser()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/users/{userId}/name",
            new UpdateUserNameRequest("Updated", "Name"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await HttpClient.GetAsync($"api/v1/users/{userId}");
        var body = await getResponse.Content.ReadFromJsonAsync<UserResponse>();
        body!.FirstName.Should().Be("Updated");
        body.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task UpdateName_ShouldReturnForbidden_WhenCallerIsAnUnrelatedUser()
    {
        // Arrange
        var (_, _, targetUserId) = await RegisterAndAuthenticateAsync();
        var (callerAccessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(callerAccessToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/users/{targetUserId}/name",
            new UpdateUserNameRequest("Hacked", "Name"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateName_ShouldReturnUnauthorized_WhenNoTokenIsProvided()
    {
        // Arrange
        var (_, _, userId) = await RegisterAndAuthenticateAsync();

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/users/{userId}/name",
            new UpdateUserNameRequest("New", "Name"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "LastName")]
    [InlineData("FirstName", "")]
    public async Task UpdateName_ShouldReturnBadRequest_WhenRequestIsInvalid(string firstName, string lastName)
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/users/{userId}/name",
            new UpdateUserNameRequest(firstName, lastName));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}