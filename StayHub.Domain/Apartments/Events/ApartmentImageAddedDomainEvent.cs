using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record ApartmentImageAddedDomainEvent(Guid Id, Guid ApartmentId) : IDomainEvent;