using StayHub.Domain.Notifications;

namespace StayHub.Application.Notifications.GetMyNotifications;

public sealed class MyNotificationsResponse
{
    public Guid Id { get; init; }

    public NotificationType Type { get; init; }

    public string Payload { get; init; } = string.Empty;

    public bool IsRead { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}