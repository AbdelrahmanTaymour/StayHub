using StayHub.Domain.Apartments;

namespace StayHub.Domain.Bookings;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsOverlappingAsync(
        Apartment apartment,
        DateRange duration,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetConfirmedPastEndDateAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default);

    void Add(Booking booking);
}