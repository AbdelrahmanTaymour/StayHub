using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public sealed record ApartmentAmenitiesChangedDomainEvent(Guid ApartmentId) : IDomainEvent;