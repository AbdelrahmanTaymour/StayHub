using MediatR;
using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Users.Events;

namespace StayHub.Application.Users.UpdateUserName;

/// <summary>
/// Invalidates the user cache on any user data change. A single handler file covers
/// all user-mutating events, so there's one place to look when the cache key shape changes.
/// </summary>
public sealed class UserCacheInvalidationHandler(
    ICacheService cacheService) :
    INotificationHandler<UserNameUpdatedDomainEvent>,
    INotificationHandler<UserProfileUpdatedDomainEvent>
{
    public Task Handle(UserNameUpdatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.UserId, cancellationToken);

    public Task Handle(UserProfileUpdatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.UserId, cancellationToken);

    private async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken)
    {
        // TODO: CONCEDER TO IMPLEMENT BULK REMOVE INSTEAD


        // Invalidate the public user view (GetUserQuery - keyed by our internal Guid).
        await cacheService.RemoveAsync(CacheKeys.User(userId), cancellationToken);

        // since both query handlers project the same underlying user+profile data.
        await cacheService.RemoveAsync(CacheKeys.LoggedInUser(userId), cancellationToken);
    }
}