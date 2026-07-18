using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record ApartmentCreatedDomainEvent(Guid Id):IDomainEvent;