using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.Users.RegisterUser;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Users;

public class RegisterUserTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task RegisterUser_ShouldCreateLocalUserAndRealKeycloakIdentity_WhenEmailIsUnique()
    {
        // Arrange
        var uniqueEmail = $"{Guid.NewGuid():N}@test.local";
        var command = new RegisterUserCommand("Karim", "Hassan", uniqueEmail, "Str0ng!Passw0rd");

        // Act — hits the real Keycloak container's admin API, not a double.
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue(
            "registration should succeed, but got error {0}: {1}",
            result.IsFailure ? result.Error.Code : string.Empty,
            result.IsFailure ? result.Error.Message : string.Empty);

        DbContext.ChangeTracker.Clear();
        var persistedUser = await DbContext.Set<User>().SingleAsync(u => u.Id == result.Value);

        persistedUser.Email.Value.Should().Be(uniqueEmail);
        persistedUser.IdentityId.Should().NotBeNullOrWhiteSpace();

        var persistedProfile = await DbContext.Set<UserProfile>().SingleAsync(p => p.UserId == result.Value);
        persistedProfile.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterUser_ShouldReturnEmailNotUnique_WhenEmailAlreadyRegisteredLocally()
    {
        // Arrange — the uniqueness check is a local DB read before any
        // Keycloak call is made, so this never touches the Keycloak
        // container at all.
        var existingUser = UserTestData.CreateUser();
        DbContext.Add(existingUser);
        await DbContext.SaveChangesAsync();

        var command = new RegisterUserCommand("Karim", "Hassan", existingUser.Email.Value, "Str0ng!Passw0rd");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailNotUnique);
    }
}