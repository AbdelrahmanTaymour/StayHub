using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Users;

public class ForgotPasswordCommandHandlerTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private const string ForgotPasswordRoute = "api/v1/users/forgot-password";

    [Fact]
    public async Task ForgotPassword_ShouldReturnNoContent_WhenEmailIsRegistered()
    {
        // Arrange
        var (_, request, _) = await RegisterAndAuthenticateAsync();

        // Act
        var response = await HttpClient.PostAsJsonAsync(ForgotPasswordRoute, new { request.Email });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnNoContent_WhenEmailIsNotRegistered()
    {
        // Arrange
        var unregisteredEmail = $"{Guid.NewGuid():N}@test.local";

        // Act
        var response = await HttpClient.PostAsJsonAsync(ForgotPasswordRoute, new { Email = unregisteredEmail });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnValidationProblem_WhenEmailIsEmpty()
    {
        // Arrange

        // Act
        var response = await HttpClient.PostAsJsonAsync(ForgotPasswordRoute, new { Email = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnValidationProblem_WhenEmailIsMalformed()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync(ForgotPasswordRoute, new { Email = "not-an-email" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_ShouldActuallySendAnEmail_WhenEmailIsRegistered()
    {
        // Arrange
        var (_, request, _) = await RegisterAndAuthenticateAsync();

        // Act
        var response = await HttpClient.PostAsJsonAsync(ForgotPasswordRoute, new { request.Email });
        response.EnsureSuccessStatusCode();

        // Assert
        var message = await MailpitClient.WaitForMessageToAsync(Factory, request.Email);
        var toAddress = message.GetProperty("To")[0].GetProperty("Address").GetString();
        toAddress.Should().Be(request.Email);

        var textBody = message.GetProperty("Text").GetString();
        textBody.Should().Contain("login-actions");
    }

    [Fact]
    public async Task ForgotPassword_ShouldNotSendAnEmail_WhenEmailIsNotRegistered()
    {
        // Arrange
        var unregisteredEmail = $"{Guid.NewGuid():N}@test.local";

        // Act
        var response = await HttpClient.PostAsJsonAsync(ForgotPasswordRoute, new { Email = unregisteredEmail });
        response.EnsureSuccessStatusCode();

        // Assert
        var noMessageArrived = await MailpitClient.ConfirmNoMessageToAsync(
            Factory, unregisteredEmail, TimeSpan.FromSeconds(3));
        noMessageArrived.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_ShouldSendANewEmail_WhenRequestedTwice()
    {
        // Arrange
        var (_, request, _) = await RegisterAndAuthenticateAsync();

        var firstResponse = await HttpClient.PostAsJsonAsync(
            ForgotPasswordRoute,
            new { request.Email });

        firstResponse.EnsureSuccessStatusCode();

        await MailpitClient.WaitForMessageToAsync(
            Factory,
            request.Email);

        // Act
        var secondResponse = await HttpClient.PostAsJsonAsync(
            ForgotPasswordRoute,
            new { request.Email });

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await MailpitClient.WaitForMessageCountToAsync(
            Factory,
            request.Email,
            2);
    }
}