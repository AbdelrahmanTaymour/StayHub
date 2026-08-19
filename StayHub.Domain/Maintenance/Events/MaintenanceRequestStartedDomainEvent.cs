using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance.Events;

public record MaintenanceRequestStartedDomainEvent(Guid MaintenanceRequestId) : IDomainEvent;