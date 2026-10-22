using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Notifications.GetUnreadNotificationsCount;

public sealed record GetUnreadNotificationsCountQuery : IQuery<int>;