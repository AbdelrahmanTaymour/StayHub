namespace StayHub.Infrastructure.Outbox;

internal class OutboxOptions
{
    public int IntervalInSeconds { get; set; }

    public int BatchSize { get; set; }

    public int MaxRetries { get; set; }
}