namespace StayHub.Domain.Notifications;

public enum NotificationType
{
    BookingConfirmed = 1,
    BookingRejected = 2,
    BookingCancelled = 3,
    NewMessage = 4,
    ReviewReceived = 5,
    ReviewResponseReceived = 6,
    MaintenanceRequestCreated = 7,
    MaintenanceRequestUpdate = 8,
    ApartmentStaffAssignmentCreated = 9
}