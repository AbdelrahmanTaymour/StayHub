using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record MaintenanceRequestStartedDomainEvent(Guid Id) : IDomainEvent;