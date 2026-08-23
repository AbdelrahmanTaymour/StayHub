using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.UpdateUserName;

internal sealed class UpdateUserName(
    IUserRepository userRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateUserNameCommand>
{
    public async Task<Result> Handle(UpdateUserNameCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId != request.UserId &&
            !userContext.Roles.Contains(Role.Admin.Name))
        {
            return Result.Failure(UserErrors.NotAuthorized);
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null) return Result.Failure(UserErrors.NotFound);

        user.UpdateName(new FirstName(request.FirstName), new LastName(request.LastName));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}