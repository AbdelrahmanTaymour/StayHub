using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public sealed record ApartmentAvailabilityBlockCreatedDomainEvent(
    Guid Id,
    Guid ApartmentId,
    DateOnly Start,
    DateOnly End) : IDomainEvent;