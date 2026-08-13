using Hangfire;
using Microsoft.AspNetCore.Builder;

namespace StayHub.Infrastructure.Outbox;

/// <summary>
/// Enqueues the first run of <see cref="ProcessOutboxMessagesJob"/> on startup.
/// From then on the job reschedules itself (see ProcessOutboxMessagesJob.ProcessAsync),
/// so this only needs to fire once per application start.
/// </summary>
public static class ProcessOutboxMessagesJobSetup
{
    public static void Start(IApplicationBuilder app)
    {
        BackgroundJob.Enqueue<ProcessOutboxMessagesJob>(job => job.ProcessAsync(JobCancellationToken.Null));
    }
}