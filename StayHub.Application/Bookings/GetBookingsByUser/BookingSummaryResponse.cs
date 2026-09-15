using StayHub.Domain.Bookings;

namespace StayHub.Application.Bookings.GetBookingsByUser;

public sealed class BookingSummaryResponse
{
    public Guid Id { get; init; }

    public Guid ApartmentId { get; init; }

    public BookingStatus Status { get; init; }

    public decimal TotalPriceAmount { get; init; }

    public string TotalPriceCurrency { get; init; }

    public DateOnly DurationStart { get; init; }

    public DateOnly DurationEnd { get; init; }
}