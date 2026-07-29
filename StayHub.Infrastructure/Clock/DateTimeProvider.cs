using StayHub.Application.Abstractions.Clock;

namespace StayHub.Infrastructure.Clock;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}