using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance.Events;

public record MaintenanceRequestResolvedDomainEvent(Guid Id) : IDomainEvent;