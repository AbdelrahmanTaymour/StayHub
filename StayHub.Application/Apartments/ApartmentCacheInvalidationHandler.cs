using MediatR;
using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Apartments.Events;

namespace StayHub.Application.Apartments;

internal sealed class ApartmentCacheInvalidationHandler(ICacheService cacheService) :
    INotificationHandler<ApartmentUpdatedDomainEvent>,
    INotificationHandler<ApartmentActivatedDomainEvent>,
    INotificationHandler<ApartmentDeactivatedDomainEvent>,
    INotificationHandler<ApartmentAmenitiesChangedDomainEvent>,
    INotificationHandler<ApartmentImageAddedDomainEvent>,
    INotificationHandler<ApartmentImageUpdatedDomainEvent>,
    INotificationHandler<ApartmentImageRemovedDomainEvent>
{
    public Task Handle(ApartmentActivatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.ApartmentId, cancellationToken);

    public Task Handle(ApartmentAmenitiesChangedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.ApartmentId, cancellationToken);

    public Task Handle(ApartmentDeactivatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.ApartmentId, cancellationToken);

    public Task Handle(ApartmentImageAddedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.ApartmentId, cancellationToken);

    public Task Handle(ApartmentImageRemovedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.ApartmentId, cancellationToken);

    public Task Handle(ApartmentImageUpdatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.ApartmentId, cancellationToken);

    public Task Handle(ApartmentUpdatedDomainEvent notification, CancellationToken cancellationToken) =>
        InvalidateAsync(notification.ApartmentId, cancellationToken);

    private Task InvalidateAsync(Guid apartmentId, CancellationToken cancellationToken)
    {
        return cacheService.RemoveAsync(CacheKeys.Apartment(apartmentId), cancellationToken);
    }
}