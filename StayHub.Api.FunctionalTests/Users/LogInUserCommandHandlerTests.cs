using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.Endpoints.Users;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Abstractions.Authentication;

namespace StayHub.Api.FunctionalTests.Users;

public class LogInUserCommandHandlerTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task LogIn_ShouldReturnAccessToken_WhenCredentialsAreValid()
    {
        // Arrange
        var (_, request, _) = await RegisterAndAuthenticateAsync();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/users/login",
            new LogInUserRequest(request.Email, request.Password));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LogIn_ShouldReturnIdenticalResponse_ForWrongPasswordAndForUnknownEmail()
    {
        // Arrange
        var (_, request, _) = await RegisterAndAuthenticateAsync();

        // Act
        var wrongPasswordResponse = await HttpClient.PostAsJsonAsync(
            "api/v1/users/login",
            new LogInUserRequest(request.Email, "TotallyWrongPassword!1"));

        var unknownEmailResponse = await HttpClient.PostAsJsonAsync(
            "api/v1/users/login",
            new LogInUserRequest($"{Guid.NewGuid():N}@test.local", "TotallyWrongPassword!1"));

        // Assert
        wrongPasswordResponse.StatusCode.Should().Be(unknownEmailResponse.StatusCode);

        var wrongPasswordBody = await wrongPasswordResponse.Content.ReadAsStringAsync();
        var unknownEmailBody = await unknownEmailResponse.Content.ReadAsStringAsync();
        wrongPasswordBody.Should().Be(unknownEmailBody);
    }

    [Theory]
    [InlineData("", "Str0ng!Passw0rd")]
    [InlineData("not-an-email", "Str0ng!Passw0rd")]
    [InlineData("valid@test.local", "")]
    public async Task LogIn_ShouldReturnBadRequestWithValidationErrors_WhenRequestIsInvalid(
        string email, string password)
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/users/login",
            new LogInUserRequest(email, password));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LogIn_ShouldSucceed_WhenNotAuthenticated()
    {
        // Arrange
        var (_, request, _) = await RegisterAndAuthenticateAsync();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/users/login",
            new LogInUserRequest(request.Email, request.Password));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}