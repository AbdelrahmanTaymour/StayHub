using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record ApartmentStaffAssignmentRevokedDomainEvent(Guid ApartmentStaffAssignmentId) : IDomainEvent;