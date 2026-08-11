using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public sealed record ApartmentImageUpdatedDomainEvent(Guid ApartmentId) : IDomainEvent;