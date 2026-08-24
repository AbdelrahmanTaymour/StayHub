namespace StayHub.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool Enabled { get; set; }

    public CompleteExpiredBookingsJobOptions CompleteExpiredBookings { get; set; } = new();
}