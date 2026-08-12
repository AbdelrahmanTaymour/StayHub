using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.RevokeUserSession;

public class RevokeUserSessionCommandHandler(
    IUserSessionRepository userSessionRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RevokeUserSessionCommand>
{
    public async Task<Result> Handle(RevokeUserSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await userSessionRepository.GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null) return Result.Failure(UserSessionErrors.NotFound);

        if (session.UserId != userContext.UserId &&
            !userContext.Roles.Contains(Role.Admin.Name))
        {
            return Result.Failure(UserSessionErrors.NotAuthorized);
        }

        var result = session.Revoke(dateTimeProvider.UtcNow);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}