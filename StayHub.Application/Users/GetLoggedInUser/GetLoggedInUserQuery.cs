using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.GetLoggedInUser;

public sealed record GetLoggedInUserQuery() : IQuery<LoggedInUserResponse>;