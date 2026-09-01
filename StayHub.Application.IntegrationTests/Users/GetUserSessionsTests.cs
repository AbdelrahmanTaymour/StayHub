using FluentAssertions;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.Users.GetUserSessions;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Users;

public class GetUserSessionsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetUserSessions_ShouldReturnNotAuthorized_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        var query = new GetUserSessionsQuery(user.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserSessionErrors.NotAuthorized);
    }

    [Fact]
    public async Task GetUserSessions_ShouldExcludeRevokedSessions_AndOrderByLastSeenDescending()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow.AddDays(-1);

        var olderActiveSession = UserTestData.CreateSession(user.Id, deviceInfo: "Older Session", utcNow: baseTime);
        var newerActiveSession =
            UserTestData.CreateSession(user.Id, deviceInfo: "Newer Session", utcNow: baseTime.AddHours(2));
        var revokedSession =
            UserTestData.CreateSession(user.Id, deviceInfo: "Revoked Session", utcNow: baseTime.AddHours(1));
        revokedSession.Revoke(DateTime.UtcNow);

        DbContext.AddRange(olderActiveSession, newerActiveSession, revokedSession);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        var query = new GetUserSessionsQuery(user.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().NotContain(s => s.Id == revokedSession.Id);
        result.Value[0].Id.Should().Be(newerActiveSession.Id);
        result.Value[1].Id.Should().Be(olderActiveSession.Id);
    }
}