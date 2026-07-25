using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.UpdateUserName;

public sealed record UpdateUserNameCommand(Guid UserId, string FirstName, string LastName) : ICommand;