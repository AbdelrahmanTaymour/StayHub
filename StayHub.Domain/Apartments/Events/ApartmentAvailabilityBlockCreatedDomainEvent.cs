using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record ApartmentAvailabilityBlockCreatedDomainEvent(
    Guid Id,
    Guid ApartmentId,
    DateOnly Start,
    DateOnly End) : IDomainEvent;