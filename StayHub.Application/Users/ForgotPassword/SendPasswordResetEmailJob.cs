using Hangfire;
using StayHub.Application.Abstractions.Authentication;

namespace StayHub.Application.Users.ForgotPassword;

public class SendPasswordResetEmailJob(IAuthenticationService authenticationService)
{
    [AutomaticRetry(Attempts = 5)]
    public async Task ExecuteAsync(string identityId, CancellationToken cancellationToken = default)
    {
        var result = await authenticationService.ForgotPasswordAsync(identityId, cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to send password reset email for identity {identityId}: {result.Error.Code}");
        }
    }
}