using FluentAssertions;
using StayHub.Domain.UnitTests.Infrastructure;
using StayHub.Domain.Users;
using StayHub.Domain.Users.Events;

namespace StayHub.Domain.UnitTests.Users;

public class UserSessionTests : BaseTest
{
    private static readonly DeviceInfo TestDeviceInfo = new("Chrome on macOS");
    private static readonly IpAddress TestIpAddress = IpAddress.Create("127.0.0.1").Value;

    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;

        // Act
        var session = UserSession.Create(UserData.OwnerId, TestDeviceInfo, TestIpAddress, utcNow);

        // Assert
        session.UserId.Should().Be(UserData.OwnerId);
        session.DeviceInfo.Should().Be(TestDeviceInfo);
        session.IpAddress.Should().Be(TestIpAddress);
        session.CreatedOnUtc.Should().Be(utcNow);
        session.LastSeenOnUtc.Should().Be(utcNow);
        session.RevokedOnUtc.Should().BeNull();
    }

    [Fact]
    public void Create_Should_RaiseUserSessionCreatedDomainEvent()
    {
        // Act
        var session = UserSession.Create(UserData.OwnerId, TestDeviceInfo, TestIpAddress, DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<UserSessionCreatedDomainEvent>(session);
        domainEvent.Id.Should().Be(session.Id);
        domainEvent.UserId.Should().Be(session.UserId);
    }

    [Fact]
    public void Touch_Should_UpdateLastSeenOnUtc()
    {
        // Arrange
        var session = UserSession.Create(UserData.OwnerId, TestDeviceInfo, TestIpAddress, DateTime.UtcNow);
        var laterUtcNow = DateTime.UtcNow.AddHours(1);

        // Act
        var result = session.Touch(laterUtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.LastSeenOnUtc.Should().Be(laterUtcNow);
    }

    [Fact]
    public void Touch_Should_Succeed_WhenTimestampEqualsLastSeenOnUtc()
    {
        // Arrange
        var utcNow = new DateTime(
            2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var session = UserSession.Create(
            UserData.OwnerId,
            TestDeviceInfo,
            TestIpAddress,
            utcNow);

        // Act
        var result = session.Touch(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.LastSeenOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Touch_Should_ReturnFailure_WhenSessionIsRevoked()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var session = UserSession.Create(
            UserData.OwnerId,
            TestDeviceInfo,
            TestIpAddress,
            utcNow);

        session.Revoke(utcNow);

        // Act
        var result = session.Touch(utcNow.AddMinutes(5));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserSessionErrors.Revoked);
    }

    [Fact]
    public void Touch_Should_ReturnFailure_WhenTimestampIsBeforeLastSeenOnUtc()
    {
        // Arrange
        var lastSeenOnUtc = new DateTime(
            2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var session = UserSession.Create(
            UserData.OwnerId,
            TestDeviceInfo,
            TestIpAddress,
            lastSeenOnUtc);

        var earlierTimestamp = lastSeenOnUtc.AddMinutes(-1);

        // Act
        var result = session.Touch(earlierTimestamp);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserSessionErrors.InvalidTimestamp);
        session.LastSeenOnUtc.Should().Be(lastSeenOnUtc);
    }

    [Fact]
    public void Revoke_Should_ReturnFailure_WhenTimestampIsBeforeCreatedOnUtc()
    {
        // Arrange
        var createdOnUtc = new DateTime(
            2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var session = UserSession.Create(
            UserData.OwnerId,
            TestDeviceInfo,
            TestIpAddress,
            createdOnUtc);

        var earlierTimestamp = createdOnUtc.AddMinutes(-1);

        // Act
        var result = session.Revoke(earlierTimestamp);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserSessionErrors.InvalidTimestamp);
        session.RevokedOnUtc.Should().BeNull();
    }


    [Fact]
    public void Revoke_Should_SetRevokedOnUtc()
    {
        // Arrange
        var session = UserSession.Create(UserData.OwnerId, TestDeviceInfo, TestIpAddress, DateTime.UtcNow);
        var utcNow = DateTime.UtcNow;

        // Act
        var result = session.Revoke(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.RevokedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Revoke_Should_RaiseUserSessionRevokedDomainEvent()
    {
        // Arrange
        var session = UserSession.Create(UserData.OwnerId, TestDeviceInfo, TestIpAddress, DateTime.UtcNow);

        // Act
        session.Revoke(DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<UserSessionRevokedDomainEvent>(session);
        domainEvent.UserSessionId.Should().Be(session.Id);
        domainEvent.UserId.Should().Be(session.UserId);
    }

    [Fact]
    public void Revoke_Should_ReturnFailure_WhenAlreadyRevoked()
    {
        // Arrange
        var session = UserSession.Create(UserData.OwnerId, TestDeviceInfo, TestIpAddress, DateTime.UtcNow);
        session.Revoke(DateTime.UtcNow);

        // Act
        var result = session.Revoke(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserSessionErrors.AlreadyRevoked);
    }
}