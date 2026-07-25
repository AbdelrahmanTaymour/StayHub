using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.CreateUser;

internal sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var isEmailUnique = await userRepository.IsEmailUniqueAsync(email, cancellationToken);

        if (!isEmailUnique) return Result.Failure<Guid>(UserErrors.EmailNotUnique);

        var user = User.Create(
            new FirstName(request.FirstName),
            new LastName(request.LastName),
            email,
            dateTimeProvider.UtcNow);

        userRepository.Add(user);

        var profile = UserProfile.Create(user.Id, dateTimeProvider.UtcNow);

        userProfileRepository.Add(profile);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}