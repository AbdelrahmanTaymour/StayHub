using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Notifications;

namespace StayHub.Api.FunctionalTests.Notifications;

public sealed class GetNotificationsByUserTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.GetAsync(NotificationRoutes.Get());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldReturnEmptyList_WhenCallerHasNoNotifications()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(NotificationRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnCallersOwnNotifications_WhenTheyExist()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        await NotificationTestData.SeedNotificationAsync(Factory, userId);
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(NotificationRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().ContainSingle();
    }

    [Fact]
    public async Task Get_ShouldNotReturnAnotherUsersNotifications()
    {
        // Arrange
        var (_, _, otherUserId) = await RegisterAndAuthenticateAsync();
        await NotificationTestData.SeedNotificationAsync(Factory, otherUserId);

        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(NotificationRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnOnlyUnreadNotifications_WhenUnreadOnlyIsTrue()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        var readNotificationId = await NotificationTestData.SeedNotificationAsync(Factory, userId);
        await NotificationTestData.SeedNotificationAsync(Factory, userId, NotificationType.NewMessage);
        AuthenticateAs(accessToken);
        var markReadResponse = await HttpClient.PostAsync(NotificationRoutes.MarkAsRead(readNotificationId), null);
        markReadResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(NotificationRoutes.Get("unreadOnly=true"));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().ContainSingle();
    }
}