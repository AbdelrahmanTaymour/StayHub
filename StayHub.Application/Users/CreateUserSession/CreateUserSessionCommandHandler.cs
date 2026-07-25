using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.CreateUserSession;

internal sealed class CreateUserSessionCommandHandler(
    IUserRepository userRepository,
    IUserSessionRepository userSessionRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateUserSessionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserSessionCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null) return Result.Failure<Guid>(UserErrors.NotFound);

        var session = UserSession.Create(
            request.UserId,
            new DeviceInfo(request.DeviceInfo),
            IpAddress.Create(request.IpAddress),
            dateTimeProvider.UtcNow);

        userSessionRepository.Add(session);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return session.Id;
    }
}