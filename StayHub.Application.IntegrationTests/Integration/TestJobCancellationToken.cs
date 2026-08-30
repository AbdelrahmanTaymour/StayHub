using Hangfire;

namespace StayHub.Application.IntegrationTests.Integration;

public sealed class TestJobCancellationToken : IJobCancellationToken
{
    public static readonly TestJobCancellationToken Instance = new();

    public CancellationToken ShutdownToken => CancellationToken.None;

    public void ThrowIfCancellationRequested()
    {
    }
}