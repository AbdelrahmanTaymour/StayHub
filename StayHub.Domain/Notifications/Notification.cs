using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Notifications;

public sealed class Notification : Entity
{
    private Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        string payload,
        DateTime createdOnUtc)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Payload = payload;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Payload { get; private set; }

    public bool IsRead { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }


    public static Notification Create(Guid userId, NotificationType type, string payload, DateTime createdOnUtc)
    {
        return new Notification(Guid.CreateVersion7(), userId, type, payload, createdOnUtc);
    }

    public Result MarkAsRead()
    {
        if (IsRead) return Result.Failure(NotificationErrors.AlreadyRead);

        IsRead = true;

        return Result.Success();
    }
}