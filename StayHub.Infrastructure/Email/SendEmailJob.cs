using Hangfire;

namespace StayHub.Infrastructure.Email;

internal sealed class SendEmailJob(SmtpEmailSender smtpEmailSender)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(
        string toAddress,
        string subject,
        string body,
        IJobCancellationToken cancellationToken)
    {
        await smtpEmailSender.SendAsync(
            toAddress,
            subject,
            body,
            cancellationToken.ShutdownToken);
    }
}