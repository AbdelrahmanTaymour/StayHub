using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.Endpoints.Users;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Users;

public class UpdateUserProfileCommandHandlerTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task UpdateProfile_ShouldReturnNoContent_WhenRequestIsValid()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/users/{userId}/profile",
            new UpdateUserProfileRequest("https://example.com/avatar.png", "A short bio.", "+15551234567"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnBadRequestWithValidationErrorsShape_WhenBioExceedsMaxLength()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var oversizedBio = new string('a', 1001);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/users/{userId}/profile",
            new UpdateUserProfileRequest(null, oversizedBio, null));

        // Assert 
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("errors");
    }


    [Fact]
    public async Task UpdateProfile_ShouldReturnForbidden_WhenCallerIsAnUnrelatedUser()
    {
        // Arrange
        var (_, _, targetUserId) = await RegisterAndAuthenticateAsync();
        var (callerAccessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(callerAccessToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/users/{targetUserId}/profile",
            new UpdateUserProfileRequest("https://example.com/hacked.png", null, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}