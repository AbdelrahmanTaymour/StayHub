using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Notifications.MarkNotificationAsRead;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Notifications;

public class MarkNotificationAsReadTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task MarkNotificationAsRead_ShouldPersistReadState_WhenCallerIsTheRecipient()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var notification = Notification.Create(user.Id, NotificationType.NewMessage, "{}", DateTime.UtcNow);
        DbContext.Add(notification);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        var command = new MarkNotificationAsReadCommand(notification.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Set<Notification>().SingleAsync(n => n.Id == notification.Id);
        persisted.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkNotificationAsRead_ShouldReturnNotAuthorized_WhenCallerIsNotTheRecipient()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var notification = Notification.Create(user.Id, NotificationType.NewMessage, "{}", DateTime.UtcNow);
        DbContext.Add(notification);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        var command = new MarkNotificationAsReadCommand(notification.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotAuthorized);
    }
}