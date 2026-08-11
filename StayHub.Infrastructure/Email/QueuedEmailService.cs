using Hangfire;
using StayHub.Application.Abstractions.Email;

namespace StayHub.Infrastructure.Email;

internal sealed class QueuedEmailService(
    IBackgroundJobClient backgroundJobClient) : IEmailService
{
    public Task SendAsync(
        Domain.Users.Email email,
        string subject,
        string body)
    {
        backgroundJobClient.Enqueue<SendEmailJob>(job => job.ExecuteAsync(
            email.Value,
            subject,
            body,
            JobCancellationToken.Null));

        return Task.CompletedTask;
    }
}