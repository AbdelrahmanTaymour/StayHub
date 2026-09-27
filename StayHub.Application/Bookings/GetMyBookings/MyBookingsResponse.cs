using StayHub.Domain.Bookings;

namespace StayHub.Application.Bookings.GetMyBookings;

public sealed record MyBookingsResponse
{
    public Guid Id { get; init; }

    public Guid ApartmentId { get; init; }

    public string ApartmentName { get; init; } = string.Empty;

    public string ApartmentCity { get; init; } = string.Empty;

    public string? PrimaryImageUrl { get; init; }

    public BookingStatus Status { get; init; }

    public decimal PricePerNight { get; init; }

    public decimal TotalPriceAmount { get; init; }

    public string TotalPriceCurrency { get; init; } = string.Empty;

    public DateOnly DurationStart { get; init; }

    public DateOnly DurationEnd { get; init; }

    public int Nights { get; init; }

    public bool CanCancel { get; init; }
}