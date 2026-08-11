using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Infrastructure.Repositories;

internal sealed class BookingRepository(ApplicationDbContext dbContext)
    : Repository<Booking>(dbContext), IBookingRepository
{
    private static readonly IReadOnlyCollection<BookingStatus> ActiveBookingStatuses =
    [
        BookingStatus.Reserved,
        BookingStatus.Confirmed
    ];

    public async Task<bool> IsOverlappingAsync(
        Apartment apartment,
        DateRange duration,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Booking>()
            .AnyAsync(
                booking =>
                    booking.ApartmentId == apartment.Id &&
                    booking.Duration.Start <= duration.End &&
                    booking.Duration.End >= duration.Start &&
                    ActiveBookingStatuses.Contains(booking.Status),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetConfirmedPastEndDateAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Booking>()
            .Where(booking => booking.Status == BookingStatus.Confirmed && booking.Duration.End <= asOf)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveBookingAsync(
        Guid apartmentId,
        Guid userId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = DateOnly.FromDateTime(asOfUtc);

        return await DbContext
            .Set<Booking>()
            .AnyAsync(
                booking =>
                    booking.ApartmentId == apartmentId &&
                    booking.UserId == userId &&
                    booking.Duration.End >= asOfDate &&
                    ActiveBookingStatuses.Contains(booking.Status),
                cancellationToken);
    }
}