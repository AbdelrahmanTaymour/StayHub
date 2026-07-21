namespace StayHub.Domain.Notifications;

public enum NotificationType
{
    BookingConfirmed = 1,
    BookingRejected = 2,
    BookingCancelled = 3,
    NewMessage = 4,
    ReviewReceived = 5,
    MaintenanceUpdate = 6
}