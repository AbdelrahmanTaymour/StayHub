using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.RegisterUser;

internal sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IAuthenticationService authenticationService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        if (email.IsFailure) return Result.Failure<Guid>(email.Error);

        var isEmailUnique = await userRepository.IsEmailUniqueAsync(email.Value, cancellationToken);

        if (!isEmailUnique) return Result.Failure<Guid>(UserErrors.EmailNotUnique);

        var user = User.Create(
            new FirstName(request.FirstName),
            new LastName(request.LastName),
            email.Value,
            dateTimeProvider.UtcNow);

        var identityIdResult = await authenticationService.RegisterAsync(
            user,
            request.Password,
            cancellationToken);

        if (identityIdResult.IsFailure) return Result.Failure<Guid>(identityIdResult.Error);

        user.SetIdentityId(identityIdResult.Value);

        userRepository.Add(user);

        var profile = UserProfile.Create(user.Id, dateTimeProvider.UtcNow);

        userProfileRepository.Add(profile);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}