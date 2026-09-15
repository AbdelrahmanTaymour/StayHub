using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.Users.ForgotPassword;
using StayHub.Application.Users.RegisterUser;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Users;

public class ForgotPasswordCommandHandlerTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ForgotPassword_ShouldReturnSuccess_WhenUserDoesNotExist()
    {
        // Arrange
        var command = new ForgotPasswordCommand($"{Guid.NewGuid():N}@test.local");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnSuccess_WhenUserDoesNotHaveIdentityId()
    {
        // Arrange
        var user = UserTestData.CreateUser();

        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var command = new ForgotPasswordCommand(user.Email.Value);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnSuccess_WhenUserHasRealKeycloakIdentity()
    {
        // Arrange
        var email = $"{Guid.NewGuid():N}@test.local";

        var registerResult = await Sender.Send(
            new RegisterUserCommand("Karim", "Hassan", email, "Str0ng!Passw0rd"));

        registerResult.IsSuccess.Should().BeTrue(
            "the test requires a real Keycloak identity before requesting a password reset");

        var user = await DbContext.Set<User>()
            .SingleAsync(u => u.Id == registerResult.Value);

        user.IdentityId.Should().NotBeNullOrWhiteSpace();

        var command = new ForgotPasswordCommand(email);

        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue(
            "password reset should succeed, but got error {0}: {1}",
            result.IsFailure ? result.Error.Code : string.Empty,
            result.IsFailure ? result.Error.Message : string.Empty);
    }
}