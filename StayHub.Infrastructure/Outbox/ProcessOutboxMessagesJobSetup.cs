using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace StayHub.Infrastructure.Outbox;

/// <summary>
/// Enqueues the first run of <see cref="ProcessOutboxMessagesJob"/> on startup.
/// From then on the job reschedules itself (see ProcessOutboxMessagesJob.ProcessAsync),
/// so this only needs to fire once per application starts.
/// </summary>
public static class ProcessOutboxMessagesJobSetup
{
    public static void Start(IApplicationBuilder app)
    {
        var backgroundJobClient = app.ApplicationServices.GetRequiredService<IBackgroundJobClient>();

        backgroundJobClient.Enqueue<ProcessOutboxMessagesJob>(job => job.ProcessAsync(JobCancellationToken.Null));
    }
}