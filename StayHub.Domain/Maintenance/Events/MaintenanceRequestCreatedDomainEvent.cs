using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance.Events;

public sealed record MaintenanceRequestCreatedDomainEvent(Guid MaintenanceRequestId, Guid ReportedByUserId)
    : IDomainEvent;