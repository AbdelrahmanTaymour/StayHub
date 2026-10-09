using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;

namespace StayHub.Application.Bookings.GetBookingsByUser;

public sealed class BookingSummaryResponse
{
    public Guid Id { get; init; }

    public Guid ApartmentId { get; init; }

    public BookingStatus Status { get; init; }

    public PaymentStatus? PaymentStatus { get; init; }

    public decimal TotalPriceAmount { get; init; }

    public string TotalPriceCurrency { get; init; } = string.Empty;

    public DateOnly DurationStart { get; init; }

    public DateOnly DurationEnd { get; init; }
}