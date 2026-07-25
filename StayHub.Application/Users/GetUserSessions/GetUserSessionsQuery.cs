using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.GetUserSessions;

public sealed record GetUserSessionsQuery(Guid UserId) : IQuery<IReadOnlyList<UserSessionResponse>>;