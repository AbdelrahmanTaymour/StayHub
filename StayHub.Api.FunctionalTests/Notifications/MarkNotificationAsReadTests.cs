using System.Net;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Notifications;

public sealed class MarkNotificationAsReadTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task MarkAsRead_ShouldReturnNoContent_WhenCallerOwnsTheNotification()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        var notificationId = await NotificationTestData.SeedNotificationAsync(Factory, userId);
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(NotificationRoutes.MarkAsRead(notificationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnForbidden_WhenCallerDoesNotOwnTheNotification()
    {
        // Arrange
        var (_, _, ownerUserId) = await RegisterAndAuthenticateAsync();
        var notificationId = await NotificationTestData.SeedNotificationAsync(Factory, ownerUserId);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(NotificationRoutes.MarkAsRead(notificationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Notification.NotAuthorized");
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(NotificationRoutes.MarkAsRead(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnConflict_WhenAlreadyMarkedAsRead()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        var notificationId = await NotificationTestData.SeedNotificationAsync(Factory, userId);
        AuthenticateAs(accessToken);
        var firstMarkAsRead = await HttpClient.PostAsync(NotificationRoutes.MarkAsRead(notificationId), null);
        firstMarkAsRead.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsync(NotificationRoutes.MarkAsRead(notificationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Notification.AlreadyRead");
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync(NotificationRoutes.MarkAsRead(notificationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}