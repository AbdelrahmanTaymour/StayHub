using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.ChangeApartmentStaffRole;

public sealed record ApartmentStaffAssignmentRoleChangedNotificationPayload(
    Guid AssignmentId,
    Guid ApartmentId,
    Guid ApartmentOwnerId,
    ApartmentStaffRole PreviousRole,
    ApartmentStaffRole NewRole,
    string Message);