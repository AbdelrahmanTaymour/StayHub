using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance.Events;

public sealed record MaintenanceRequestResolvedDomainEvent(Guid MaintenanceRequestId) : IDomainEvent;