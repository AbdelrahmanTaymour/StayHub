using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;

namespace StayHub.Application.Bookings.GetApartmentBookings;

public sealed record ApartmentBookingResponse
{
    public Guid Id { get; init; }

    public Guid GuestId { get; init; }
    public string GuestFullName { get; init; } = string.Empty;
    public string? GuestAvatarUrl { get; init; }

    public BookingStatus Status { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }

    public DateOnly DurationStart { get; init; }
    public DateOnly DurationEnd { get; init; }
    public int Nights { get; init; }

    public decimal TotalPriceAmount { get; init; }
    public string TotalPriceCurrency { get; init; } = string.Empty;

    public DateTime CreatedOnUtc { get; init; }
}