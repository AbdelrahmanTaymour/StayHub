using System.Collections.Concurrent;
using System.Linq.Expressions;
using StayHub.Application.Abstractions.BackgroundJobs;

namespace StayHub.Application.IntegrationTests.Integration;

public sealed record EnqueuedJob(Type JobType, string MethodCall, string JobId);

public sealed class TestBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly ConcurrentBag<EnqueuedJob> _enqueuedJobs = new();

    public IReadOnlyCollection<EnqueuedJob> EnqueuedJobs => _enqueuedJobs.ToArray();

    public string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall)
    {
        var jobId = Guid.NewGuid().ToString();

        _enqueuedJobs.Add(new EnqueuedJob(typeof(TJob), methodCall.ToString(), jobId));

        return jobId;
    }
}