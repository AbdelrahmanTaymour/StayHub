using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.UpdateUserName;

public sealed record UpdateUserNameCommand(string FirstName, string LastName) : ICommand;