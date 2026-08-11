using MediatR;
using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Apartments.Events;

namespace StayHub.Application.Apartments.UpdateApartment;

public sealed class ApartmentCacheInvalidationHandler(ICacheService cacheService) :
    INotificationHandler<ApartmentCreatedDomainEvent>,
    INotificationHandler<ApartmentUpdatedDomainEvent>,
    INotificationHandler<ApartmentActivatedDomainEvent>,
    INotificationHandler<ApartmentDeactivatedDomainEvent>,
    INotificationHandler<ApartmentAmenitiesChangedDomainEvent>,
    INotificationHandler<ApartmentImageAddedDomainEvent>,
    INotificationHandler<ApartmentImageUpdatedDomainEvent>,
    INotificationHandler<ApartmentImageRemovedDomainEvent>
{
    public Task Handle(ApartmentActivatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return RemoveAsync(notification.ApartmentId, cancellationToken);
    }

    public Task Handle(ApartmentAmenitiesChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        return RemoveAsync(notification.ApartmentId, cancellationToken);
    }

    public Task Handle(ApartmentCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return RemoveAsync(notification.Id, cancellationToken);
    }

    public Task Handle(ApartmentDeactivatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return RemoveAsync(notification.Id, cancellationToken);
    }

    public Task Handle(ApartmentImageAddedDomainEvent notification, CancellationToken cancellationToken)
    {
        return RemoveAsync(notification.ApartmentId, cancellationToken);
    }

    public Task Handle(ApartmentImageRemovedDomainEvent notification, CancellationToken cancellationToken)
    {
        return RemoveAsync(notification.ApartmentId, cancellationToken);
    }

    public Task Handle(ApartmentImageUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return RemoveAsync(notification.ApartmentId, cancellationToken);
    }

    public Task Handle(ApartmentUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return RemoveAsync(notification.ApartmentId, cancellationToken);
    }

    private Task RemoveAsync(Guid apartmentId, CancellationToken cancellationToken)
    {
        return cacheService.RemoveAsync(CacheKeys.Apartment(apartmentId), cancellationToken);
    }
}