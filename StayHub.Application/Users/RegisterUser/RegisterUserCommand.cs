using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.RegisterUser;

public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : ICommand<Guid>;