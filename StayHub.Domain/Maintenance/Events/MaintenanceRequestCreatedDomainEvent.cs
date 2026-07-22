using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance.Events;

public record MaintenanceRequestCreatedDomainEvent(Guid MaintenanceRequestId) : IDomainEvent;