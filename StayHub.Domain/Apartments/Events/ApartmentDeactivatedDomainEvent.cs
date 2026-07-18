using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record ApartmentDeactivatedDomainEvent(Guid Id) : IDomainEvent;