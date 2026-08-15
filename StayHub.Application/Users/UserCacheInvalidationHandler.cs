using MediatR;
using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Users.Events;

namespace StayHub.Application.Users;

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
        // TODO: IMPLEMENT BULK REMOVE INSTEAD

        await cacheService.RemoveAsync(CacheKeys.User(userId), cancellationToken);
        await cacheService.RemoveAsync(CacheKeys.LoggedInUser(userId), cancellationToken);
    }
}