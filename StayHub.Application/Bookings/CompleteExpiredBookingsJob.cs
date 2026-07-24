using StayHub.Application.Abstractions.Clock;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Bookings;

public sealed class CompleteExpiredBookingsJob(
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = dateTimeProvider.UtcNow;

        var bookings = await bookingRepository.GetConfirmedPastEndDateAsync(
            DateOnly.FromDateTime(utcNow),
            cancellationToken);

        foreach (var booking in bookings) booking.Complete(utcNow);

        if (bookings.Count > 0) await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}