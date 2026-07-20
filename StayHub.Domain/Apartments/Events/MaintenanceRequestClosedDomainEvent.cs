using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record MaintenanceRequestClosedDomainEvent(Guid Id) : IDomainEvent;