using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Notifications;

namespace StayHub.Infrastructure.Repositories;

internal sealed class NotificationRepository(ApplicationDbContext dbContext)
    : Repository<Notification>(dbContext), INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext
            .Set<Notification>()
            .Where(notification => notification.UserId == userId);

        if (unreadOnly) query = query.Where(notification => !notification.IsRead);

        return await query
            .OrderByDescending(notification => notification.CreatedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}