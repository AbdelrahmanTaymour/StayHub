using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance.Events;

public sealed record MaintenanceRequestStartedDomainEvent(Guid MaintenanceRequestId) : IDomainEvent;