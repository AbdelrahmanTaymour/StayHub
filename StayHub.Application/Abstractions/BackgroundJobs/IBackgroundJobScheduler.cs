using System.Linq.Expressions;

namespace StayHub.Application.Abstractions.BackgroundJobs;

public interface IBackgroundJobScheduler
{
    string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall);
}