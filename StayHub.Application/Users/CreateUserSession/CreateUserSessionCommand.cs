using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.CreateUserSession;

public sealed record CreateUserSessionCommand(
    Guid UserId,
    string DeviceInfo,
    string IpAddress) : ICommand<Guid>;