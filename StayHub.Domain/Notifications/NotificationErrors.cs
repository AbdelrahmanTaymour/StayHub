using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Notifications;

public static class NotificationErrors
{
    public static Error NotFound = new(
        "Notification.NotFound",
        "The notification with the specified identifier was not found");

    public static Error AlreadyRead = new(
        "Notification.AlreadyRead",
        "The notification has already been marked as read");

    public static Error NotAuthorized = new(
        "Notification.NotAuthorized",
        "You can only mark your own notifications as read");
}