using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.UnitTests.Apartments;

namespace StayHub.Domain.UnitTests.Bookings;

internal static class BookingData
{
    public static readonly DateRange Duration =
        DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));

    public static Booking Reserve(Apartment? apartment = null, DateRange? duration = null, Guid? userId = null)
    {
        return Booking.Reserve(
            apartment ?? ApartmentData.Create(),
            userId ?? Guid.CreateVersion7(),
            duration ?? Duration,
            new PricingService(),
            DateTime.UtcNow).Value;
    }

    public static Booking ReserveAndConfirm(Apartment? apartment = null, DateRange? duration = null)
    {
        var booking = Reserve(apartment, duration);
        booking.Confirm(DateTime.UtcNow);
        return booking;
    }

    public static Booking RejectedBooking()
    {
        var booking = Reserve();
        booking.Reject(Guid.CreateVersion7(), DateTime.UtcNow);
        return booking;
    }

    public static Booking CompletedBooking()
    {
        var booking = ReserveAndConfirm();
        booking.Complete(DateTime.UtcNow);
        return booking;
    }

    public static Booking AlreadyCancelledBooking()
    {
        var booking = ReserveAndConfirm();
        booking.Cancel(new DateTime(2025, 12, 31));
        return booking;
    }
}