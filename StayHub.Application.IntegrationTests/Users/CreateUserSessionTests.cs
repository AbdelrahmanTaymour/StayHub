using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.Users.CreateUserSession;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Users;

public class CreateUserSessionTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateUserSession_ShouldPersistSessionAndSendAlertEmail_ViaOutboxPipeline()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var command = new CreateUserSessionCommand(user.Id, "Chrome on macOS", "198.51.100.23");

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        DbContext.ChangeTracker.Clear();
        var persistedSession = await DbContext.Set<UserSession>().SingleAsync(s => s.Id == result.Value);
        persistedSession.UserId.Should().Be(user.Id);

        EmailService.SentEmails.Should().ContainSingle(e =>
            e.To.Value == user.Email.Value &&
            e.Subject == "New sign-in to your account");
    }
}