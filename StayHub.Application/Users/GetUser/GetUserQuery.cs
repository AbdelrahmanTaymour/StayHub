using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.GetUser;

public sealed record GetUserQuery(Guid UserId) : IQuery<UserResponse>;