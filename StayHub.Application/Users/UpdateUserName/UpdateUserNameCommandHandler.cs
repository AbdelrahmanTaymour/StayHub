using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.UpdateUserName;

internal sealed class UpdateUserNameCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateUserNameCommand>
{
    public async Task<Result> Handle(UpdateUserNameCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null) return Result.Failure(UserErrors.NotFound);

        user.UpdateName(new FirstName(request.FirstName), new LastName(request.LastName));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}