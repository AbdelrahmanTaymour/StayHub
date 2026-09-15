using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.InviteUser;

public sealed record InviteUserCommand(Guid ApartmentId, string Email, string Body) : ICommand;