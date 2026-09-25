using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.IntegrationTests.Bookings;

internal static class BookingTestData
{
    public static Booking Reserve(
        Apartment apartment,
        Guid userId,
        DateOnly start,
        DateOnly end,
        PricingService pricingService,
        DateTime? utcNow = null)
    {
        var duration = DateRange.Create(start, end);

        var result = Booking.Reserve(apartment, userId, duration, pricingService, utcNow ?? DateTime.UtcNow);

        return result.Value;
    }

    public static Booking ReserveAndConfirm(
        Apartment apartment,
        Guid userId,
        DateOnly start,
        DateOnly end,
        PricingService pricingService,
        DateTime? utcNow = null)
    {
        var currentDate = utcNow ?? DateTime.UtcNow;
        var duration = DateRange.Create(start, end);

        var result = Booking.Reserve(apartment, userId, duration, pricingService, currentDate);

        result.Value.Confirm(currentDate);

        return result.Value;
    }
}