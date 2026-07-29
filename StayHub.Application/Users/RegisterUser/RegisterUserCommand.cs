using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.CreateUser;

public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : ICommand<Guid>;