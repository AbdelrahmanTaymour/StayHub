using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Notifications;

namespace StayHub.Api.FunctionalTests.Notifications;

public sealed class GetNotificationsByUserTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Get_ShouldReturnEmptyPagedEnvelope_WhenCallerHasNoNotifications()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(NotificationRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Get_ShouldReturnCallersOwnNotifications_WhenTheyExist()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        await NotificationTestFixtures.SeedNotificationAsync(Factory, userId);
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(NotificationRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_ShouldNotReturnAnotherUsersNotifications()
    {
        // Arrange
        var (_, _, otherUserId) = await RegisterAndAuthenticateAsync();
        await NotificationTestFixtures.SeedNotificationAsync(Factory, otherUserId);

        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(NotificationRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnOnlyUnreadNotifications_WhenUnreadOnlyIsTrue()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        var readNotificationId = await NotificationTestFixtures.SeedNotificationAsync(Factory, userId);
        await NotificationTestFixtures.SeedNotificationAsync(Factory, userId, NotificationType.NewMessage);
        AuthenticateAs(accessToken);
        var markReadResponse = await HttpClient.PostAsync(NotificationRoutes.MarkAsRead(readNotificationId), null);
        markReadResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(NotificationRoutes.Get("unreadOnly=true"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
    }

    private sealed record PagedResponseDto<T>(
        List<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}