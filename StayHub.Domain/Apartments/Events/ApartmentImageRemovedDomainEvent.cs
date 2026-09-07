using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public sealed record ApartmentImageRemovedDomainEvent(Guid ImageId, Guid ApartmentId, ImageUrl Url) : IDomainEvent;