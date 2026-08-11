using StayHub.Domain.Apartments;

namespace StayHub.Domain.Bookings;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsOverlappingAsync(
        Apartment apartment,
        DateRange duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether userId already has a current (not yet ended) Reserved or
    ///     Confirmed booking for apartmentId, as of asOfUtc. Intended to stop a
    ///     guest from opening a second concurrent booking on an apartment they're
    ///     already booked into — a Completed (past, checked-out) booking does NOT
    ///     count, since it doesn't preclude booking again.
    /// </summary>
    Task<bool> HasActiveBookingAsync(
        Guid apartmentId,
        Guid userId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetConfirmedPastEndDateAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default);

    void Add(Booking booking);
}