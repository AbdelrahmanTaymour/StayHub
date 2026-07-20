using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record MaintenanceRequestCreatedDomainEvent(Guid MaintenanceRequestId) : IDomainEvent;