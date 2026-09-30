using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Notifications.GetMyNotifications;

public sealed record GetMyNotificationsQuery(
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResponse<MyNotificationsResponse>>;