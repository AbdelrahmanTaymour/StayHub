using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Notifications;

public static class NotificationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Notification.NotFound",
        "The notification with the specified identifier was not found");

    public static readonly Error AlreadyRead = Error.Conflict(
        "Notification.AlreadyRead",
        "The notification has already been marked as read");

    public static readonly Error NotAuthorized = Error.Forbidden(
        "Notification.NotAuthorized",
        "You can only mark your own notifications as read");
}