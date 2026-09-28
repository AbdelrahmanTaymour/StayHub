using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public sealed record ApartmentDeactivatedDomainEvent(Guid ApartmentId, Guid OwnerId) : IDomainEvent;