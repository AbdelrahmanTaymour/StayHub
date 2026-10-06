using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.UpdateUserAvatar;

internal sealed class UpdateUserAvatarCommandHandler(
    IUserProfileRepository userProfileRepository,
    IUserContext userContext,
    IFileStorageService fileStorageService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider
) : ICommandHandler<UpdateUserAvatarCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UpdateUserAvatarCommand request, CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByUserIdAsync(userContext.UserId, cancellationToken);

        if (profile is null) return Result.Failure<Guid>(UserProfileErrors.NotFound);

        var avatarKey = await fileStorageService.UploadAsync(
            request.FileContent,
            request.FileName,
            request.ContentType,
            ImageCategory.UserAvatar,
            cancellationToken);

        if (profile.AvatarKey != null)
        {
            await fileStorageService.DeleteAsync(profile.AvatarKey.key, cancellationToken);
        }

        profile.UpdateAvatar(new AvatarKey(avatarKey), dateTimeProvider.UtcNow);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return profile.Id;
        }
        catch
        {
            // Clean up orphan file in cloud/storage if database commit fails
            await fileStorageService.DeleteAsync(avatarKey, cancellationToken);
            throw;
        }
    }
}