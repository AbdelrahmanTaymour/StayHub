using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record ApartmentImageRemovedDomainEvent(Guid ImageId, Guid ApartmentId, ImageUrl Url) : IDomainEvent;