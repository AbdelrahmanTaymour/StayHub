using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public sealed record ApartmentStaffAssignmentRoleChangedDomainEvent(
    Guid AssignmentId,
    Guid ApartmentId,
    Guid UserId,
    ApartmentStaffRole PreviousRole,
    ApartmentStaffRole NewRole)
    : IDomainEvent;