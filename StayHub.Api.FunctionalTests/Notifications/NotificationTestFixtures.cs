using Microsoft.Extensions.DependencyInjection;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Notifications;
using StayHub.Infrastructure;

namespace StayHub.Api.FunctionalTests.Notifications;

internal static class NotificationTestFixtures
{
    internal static async Task<Guid> SeedNotificationAsync(
        FunctionalTestWebAppFactory factory,
        Guid userId,
        NotificationType type = NotificationType.BookingConfirmed,
        string payload = "{}")
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var notification = Notification.Create(userId, type, payload, DateTime.UtcNow);

        dbContext.Set<Notification>().Add(notification);
        await dbContext.SaveChangesAsync();

        return notification.Id;
    }
}