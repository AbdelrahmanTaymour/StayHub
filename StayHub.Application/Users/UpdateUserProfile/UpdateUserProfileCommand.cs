using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string? AvatarUrl,
    string? Bio,
    string? PhoneNumber) : ICommand;