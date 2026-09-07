using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance.Events;

public sealed record MaintenanceRequestClosedDomainEvent(Guid MaintenanceRequestId) : IDomainEvent;