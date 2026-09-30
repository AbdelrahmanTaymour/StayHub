using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Users.GetOwnerProfile;

namespace StayHub.Api.FunctionalTests.Users;

public class GetOwnerProfileTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetOwnerProfile_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Arrange
        var (_, _, ownerId) = await RegisterAndAuthenticateAsync();

        // Act
        var response = await HttpClient.GetAsync($"api/v1/users/{ownerId}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        body!.Id.Should().Be(ownerId);
    }

    [Fact]
    public async Task GetOwnerProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Act
        var response = await HttpClient.GetAsync($"api/v1/users/{Guid.NewGuid()}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOwnerProfile_ShouldNotExposeEmailOrPhoneNumber()
    {
        // Arrange
        var (_, _, ownerId) = await RegisterAndAuthenticateAsync();

        // Act
        var response = await HttpClient.GetAsync($"api/v1/users/{ownerId}/profile");
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("email");
        body.Should().NotContain("phoneNumber");
    }
}