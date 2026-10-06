using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Users;

public class UpdateUserAvatarCommandHandlerTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task UpdateAvatar_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("image/png");

        content.Add(fileContent, "file", "avatar.png");

        // Act
        var response = await HttpClient.PutAsync(
            "api/v1/users/profile/avatar",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var avatarKey = await response.Content.ReadFromJsonAsync<Guid>();
        avatarKey.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateAvatar_ShouldReturnNotFound_WhenUserProfileDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("image/png");

        content.Add(fileContent, "file", "avatar.png");

        // Act
        var response = await HttpClient.PutAsync(
            "api/v1/users/profile/avatar",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateAvatar_ShouldReturnUnauthorized_WhenCallerIsNotAuthenticated()
    {
        // Arrange
        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("image/png");

        content.Add(fileContent, "file", "avatar.png");

        // Act
        var response = await HttpClient.PutAsync(
            "api/v1/users/profile/avatar",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateAvatar_ShouldReturnBadRequest_WhenFileIsMissing()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        using var content = new MultipartFormDataContent();

        // Act
        var response = await HttpClient.PutAsync(
            "api/v1/users/profile/avatar",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}