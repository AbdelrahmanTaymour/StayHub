using MediatR;
using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Apartments.Events;

namespace StayHub.Application.Apartments.GetMyApartments;

internal sealed class GetMyApartmentsCacheInvalidationHandler(
    ICacheService cacheService)
    : INotificationHandler<ApartmentCreatedDomainEvent>,
        INotificationHandler<ApartmentUpdatedDomainEvent>,
        INotificationHandler<ApartmentActivatedDomainEvent>,
        INotificationHandler<ApartmentDeactivatedDomainEvent>
{
    public Task Handle(ApartmentActivatedDomainEvent notification, CancellationToken cancellationToken)
        => InvalidateAsync(notification.OwnerId, cancellationToken);

    public Task Handle(ApartmentCreatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.OwnerId, cancellationToken);

    public Task Handle(ApartmentDeactivatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.OwnerId, cancellationToken);

    public Task Handle(ApartmentUpdatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.OwnerId, cancellationToken);

    private Task InvalidateAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return cacheService.IncrementAsync(CacheKeys.MyApartmentsVersion(ownerId), cancellationToken);
    }
}