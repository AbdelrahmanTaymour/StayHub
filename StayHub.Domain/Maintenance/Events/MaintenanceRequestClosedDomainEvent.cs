using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance.Events;

public record MaintenanceRequestClosedDomainEvent(Guid Id) : IDomainEvent;