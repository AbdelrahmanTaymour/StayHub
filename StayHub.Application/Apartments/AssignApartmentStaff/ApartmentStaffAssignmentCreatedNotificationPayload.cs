namespace StayHub.Application.Apartments.AssignApartmentStaff;

public sealed record ApartmentStaffAssignmentCreatedNotificationPayload(
    Guid AssignmentId,
    Guid ApartmentId,
    Guid ApartmentOwnerId,
    string Message);