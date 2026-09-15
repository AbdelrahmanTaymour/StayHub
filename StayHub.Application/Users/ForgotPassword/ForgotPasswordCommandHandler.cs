using StayHub.Application.Abstractions.BackgroundJobs;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IBackgroundJobScheduler backgroundJobScheduler) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is not null && !string.IsNullOrEmpty(user.IdentityId))
        {
            backgroundJobScheduler.Enqueue<SendPasswordResetEmailJob>(job =>
                job.ExecuteAsync(user.IdentityId, CancellationToken.None));
        }

        return Result.Success();
    }
}