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
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/users/profile",
            new UpdateUserProfileRequest("A short bio.", "+15551234567"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnBadRequestWithValidationErrorsShape_WhenBioExceedsMaxLength()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var oversizedBio = new string('a', 1001);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/users/profile",
            new UpdateUserProfileRequest(oversizedBio, null));

        // Assert 
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("errors");
    }
}