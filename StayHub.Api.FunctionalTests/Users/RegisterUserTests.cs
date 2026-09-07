using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.Endpoints.Users;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Users;

public class RegisterUserTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Register_ShouldReturnCreatedWithLocationHeader_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterUserRequest(
            "Test", "User", $"{Guid.NewGuid():N}@test.local", "Str0ng!Passw0rd");

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var userId = await response.Content.ReadFromJsonAsync<Guid>();
        userId.Should().NotBeEmpty();
        response.Headers.Location.ToString().Should().Contain(userId.ToString());
    }

    [Fact]
    public async Task Register_ShouldSucceed_WhenNotAuthenticated()
    {
        // Arrange
        var request = new RegisterUserRequest(
            "Anon", "User", $"{Guid.NewGuid():N}@test.local", "Str0ng!Passw0rd");

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenEmailIsAlreadyRegistered()
    {
        // Arrange
        var (_, firstRequest, _) = await RegisterAndAuthenticateAsync();

        var duplicateRequest = new RegisterUserRequest(
            "Someone", "Else", firstRequest.Email, "AnotherStr0ng!Pass");

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/users/register", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "User", "valid@test.local", "Str0ng!Passw0rd")]
    [InlineData("Test", "", "valid@test.local", "Str0ng!Passw0rd")]
    [InlineData("Test", "User", "not-an-email", "Str0ng!Passw0rd")]
    [InlineData("Test", "User", "valid2@test.local", "short")]
    public async Task Register_ShouldReturnBadRequestWithValidationErrors_WhenRequestIsInvalid(
        string firstName, string lastName, string email, string password)
    {
        // Arrange
        var request = new RegisterUserRequest(firstName, lastName, email, password);

        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("errors");
    }
}