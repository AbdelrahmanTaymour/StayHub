using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email) : ICommand<Guid>;