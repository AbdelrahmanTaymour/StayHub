namespace StayHub.Domain.Notifications;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(Notification notification);
}