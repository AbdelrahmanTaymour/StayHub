using FluentAssertions;
using StayHub.Domain.Notifications;

namespace StayHub.Domain.UnitTests.Notifications;

public class NotificationTests
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var utcNow = DateTime.UtcNow;
        const string payload = "{\"BookingId\":\"...\"}";

        // Act
        var notification = Notification.Create(userId, NotificationType.BookingConfirmed, payload, utcNow);

        // Assert
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(NotificationType.BookingConfirmed);
        notification.Payload.Should().Be(payload);
        notification.CreatedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Create_Should_SetIsReadToFalse()
    {
        // Act
        var notification = Notification.Create(
            Guid.CreateVersion7(),
            NotificationType.NewMessage,
            "{}",
            DateTime.UtcNow);

        // Assert
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public void MarkAsRead_Should_SetIsReadTrueAndReturnSuccess_WhenNotAlreadyRead()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.CreateVersion7(),
            NotificationType.NewMessage,
            "{}",
            DateTime.UtcNow);

        // Act
        var result = notification.MarkAsRead();

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void MarkAsRead_Should_ReturnFailure_WhenAlreadyRead()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.CreateVersion7(),
            NotificationType.NewMessage,
            "{}",
            DateTime.UtcNow);
        notification.MarkAsRead();

        // Act
        var result = notification.MarkAsRead();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.AlreadyRead);
    }
}