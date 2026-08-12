using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.UpdateUserProfile;

internal sealed class UpdateUserProfileCommandHandler(
    IUserProfileRepository userProfileRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateUserProfileCommand>
{
    public async Task<Result> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId != request.UserId &&
            !userContext.Roles.Contains(Role.Admin.Name))
        {
            return Result.Failure(UserErrors.NotAuthorized);
        }

        var profile = await userProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null) return Result.Failure(UserProfileErrors.NotFound);

        if (request.AvatarUrl is not null) profile.UpdateAvatar(new Avatar(request.AvatarUrl), dateTimeProvider.UtcNow);

        if (request.Bio is not null) profile.UpdateBio(new Bio(request.Bio), dateTimeProvider.UtcNow);

        if (request.PhoneNumber is not null)
            profile.UpdatePhoneNumber(PhoneNumber.Create(request.PhoneNumber), dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}