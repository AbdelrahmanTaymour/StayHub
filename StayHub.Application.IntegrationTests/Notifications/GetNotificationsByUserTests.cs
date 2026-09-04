using FluentAssertions;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Notifications.GetNotificationsByUser;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Notifications;

public class GetNotificationsByUserTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetNotificationsByUser_ShouldReturnEmpty_WhenUserHasNoNotifications()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetNotificationsByUserQuery(UnreadOnly: false, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotificationsByUser_ShouldOnlyReturnCurrentUsersNotifications_OrderedByCreatedOnDescending()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        var otherUser = UserTestData.CreateUser();
        DbContext.AddRange(user, otherUser);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;
        var olderNotification = Notification.Create(user.Id, NotificationType.NewMessage, "{}", baseTime);
        var newerNotification = Notification.Create(user.Id, NotificationType.NewMessage, "{}", baseTime.AddMinutes(1));
        var otherUsersNotification = Notification.Create(otherUser.Id, NotificationType.NewMessage, "{}", baseTime);
        DbContext.AddRange(olderNotification, newerNotification, otherUsersNotification);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetNotificationsByUserQuery(UnreadOnly: false, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(newerNotification.Id);
        result.Value[1].Id.Should().Be(olderNotification.Id);
    }

    [Fact]
    public async Task GetNotificationsByUser_ShouldExcludeReadNotifications_WhenUnreadOnlyIsTrue()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var unreadNotification = Notification.Create(user.Id, NotificationType.NewMessage, "{}", DateTime.UtcNow);
        var readNotification = Notification.Create(user.Id, NotificationType.NewMessage, "{}", DateTime.UtcNow);
        readNotification.MarkAsRead();
        DbContext.AddRange(unreadNotification, readNotification);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetNotificationsByUserQuery(UnreadOnly: true, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(n => n.Id == unreadNotification.Id);
    }
}