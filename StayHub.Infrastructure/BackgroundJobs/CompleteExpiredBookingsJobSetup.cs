using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StayHub.Application.Bookings;

namespace StayHub.Infrastructure.BackgroundJobs;

/// <summary>
/// Registers CompleteExpiredBookingsJob as a standard Hangfire recurring job.
/// No self-rescheduling, it's plain cron and shows up cleanly in the Hangfire
/// dashboard as a named recurring job.
/// </summary>
public static class CompleteExpiredBookingsJobSetup
{
    private const string JobId = "complete-expired-bookings";

    public static void Start(IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<CompleteExpiredBookingsJobOptions>>()
            .Value;

        RecurringJob.AddOrUpdate<CompleteExpiredBookingsJob>(
            JobId,
            job => job.ExecuteAsync(CancellationToken.None),
            options.CronExpression,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}