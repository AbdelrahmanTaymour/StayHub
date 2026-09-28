using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public sealed record ApartmentActivatedDomainEvent(Guid ApartmentId, Guid OwnerId) : IDomainEvent;