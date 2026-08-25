using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.UnitTests.Bookings;

internal static class BookingData
{
    public static readonly DateRange Duration =
        DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));

    public static Booking Reserve(Apartment apartment, Guid? userId = null)
    {
        return Booking.Reserve(apartment, userId ?? Guid.CreateVersion7(), Duration, new PricingService(),
            DateTime.UtcNow).Value;
    }

    public static Booking ReserveAndConfirm(Apartment apartment, Guid? userId = null)
    {
        var booking = Reserve(apartment, userId);
        booking.Confirm(DateTime.UtcNow);
        return booking;
    }

    public static Booking ReserveConfirmAndComplete(Apartment apartment, Guid? userId = null)
    {
        var booking = ReserveAndConfirm(apartment, userId);
        booking.Complete(DateTime.UtcNow);
        return booking;
    }
}