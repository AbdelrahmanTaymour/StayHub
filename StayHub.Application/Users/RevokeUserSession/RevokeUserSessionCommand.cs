using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.RevokeUserSession;

public sealed record RevokeUserSessionCommand(Guid SessionId) : ICommand;