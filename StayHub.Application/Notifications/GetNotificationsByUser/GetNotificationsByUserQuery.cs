using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Notifications.GetNotificationsByUser;

public sealed record GetNotificationsByUserQuery(bool UnreadOnly, int Page, int PageSize)
    : IQuery<IReadOnlyList<NotificationResponse>>;