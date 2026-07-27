using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Notifications.GetNotificationsByUser;

public sealed record GetNotificationsByUserQuery(Guid UserId, bool UnreadOnly, int Page, int PageSize)
    : IQuery<IReadOnlyList<NotificationResponse>>;