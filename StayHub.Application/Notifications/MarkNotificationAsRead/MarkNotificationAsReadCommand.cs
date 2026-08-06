using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Notifications.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : ICommand;