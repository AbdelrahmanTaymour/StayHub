using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record ApartmentStaffAssignmentCreatedDomainEvent(Guid Id, Guid ApartmentId, Guid UserId) : IDomainEvent;