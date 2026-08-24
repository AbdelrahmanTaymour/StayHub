using System.Linq.Expressions;
using Hangfire;
using StayHub.Application.Abstractions.BackgroundJobs;

namespace StayHub.Infrastructure.BackgroundJobs;

internal sealed class HangfireBackgroundJobScheduler(IBackgroundJobClient backgroundJobClient)
    : IBackgroundJobScheduler
{
    public string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall)
    {
        return backgroundJobClient.Enqueue(methodCall);
    }
}