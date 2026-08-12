using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Users.GetUser;

namespace StayHub.Application.Users.GetLoggedInUser;

public sealed record GetLoggedInUserQuery : IQuery<UserResponse>;