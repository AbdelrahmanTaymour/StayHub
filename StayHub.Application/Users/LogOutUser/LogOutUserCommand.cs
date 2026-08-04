using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.LogOutUser;

public sealed record LogOutUserCommand(string RefreshToken) : ICommand;