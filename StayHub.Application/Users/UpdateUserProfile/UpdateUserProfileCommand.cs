using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(
    string? Bio,
    string? PhoneNumber) : ICommand;