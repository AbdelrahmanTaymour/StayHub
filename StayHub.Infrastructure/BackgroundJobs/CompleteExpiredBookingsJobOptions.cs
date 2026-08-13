namespace StayHub.Infrastructure.BackgroundJobs;

internal sealed class CompleteExpiredBookingsJobOptions
{
    public const string SectionName = "BackgroundJobs:CompleteExpiredBookings";

    // Standard 5-field cron. Default: every day at 02:00 UTC — low-traffic
    // hour, and "complete a booking whose end date has passed" has no
    // sub-minute urgency, unlike outbox dispatch, so a plain recurring job
    // (not self-rescheduling) is the right fit here.
    public string CronExpression { get; set; } = "0 2 * * *";
}