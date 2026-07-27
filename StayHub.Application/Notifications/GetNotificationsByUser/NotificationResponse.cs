namespace StayHub.Application.Notifications.GetNotificationsByUser;

public sealed class NotificationResponse
{
    public Guid Id { get; init; }

    public int Type { get; init; }

    public string Payload { get; init; }

    public bool IsRead { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}