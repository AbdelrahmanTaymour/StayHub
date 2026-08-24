using Hangfire;
using StayHub.Application.Abstractions.BackgroundJobs;
using StayHub.Application.Abstractions.Email;

namespace StayHub.Infrastructure.Email;

internal sealed class EmailService(
    IBackgroundJobScheduler backgroundJobScheduler) : IEmailService
{
    public Task SendAsync(
        Domain.Users.Email email,
        string subject,
        string body)
    {
        backgroundJobScheduler.Enqueue<SendEmailJob>(job => job.ExecuteAsync(
            email.Value,
            subject,
            body,
            JobCancellationToken.Null));

        return Task.CompletedTask;
    }
}