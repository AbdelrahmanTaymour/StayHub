using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public sealed record ApartmentImageUpdatedDomainEvent(Guid ImageId, Guid ApartmentId) : IDomainEvent;