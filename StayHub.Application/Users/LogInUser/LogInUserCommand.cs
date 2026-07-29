using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.LogInUser;

public sealed record LogInUserCommand(string Email, string Password) : ICommand<AccessTokenResponse>;