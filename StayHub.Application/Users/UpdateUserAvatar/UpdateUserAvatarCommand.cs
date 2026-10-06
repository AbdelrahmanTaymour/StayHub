using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Users.UpdateUserAvatar;

public sealed record UpdateUserAvatarCommand(
    Stream FileContent,
    string FileName,
    string ContentType) : ICommand<Guid>;