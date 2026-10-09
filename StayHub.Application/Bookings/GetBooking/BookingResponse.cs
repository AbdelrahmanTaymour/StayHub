using StayHub.Application.Apartments.GetApartment;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;

namespace StayHub.Application.Bookings.GetBooking;

public sealed class BookingResponse
{
    public Guid Id { get; init; }

    public BookingStatus Status { get; init; }

    public PaymentStatus? PaymentStatus { get; init; }

    public bool CanCancel { get; init; }

    public bool HasReview { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public DateTime UpdatedOnUtc { get; init; }

    public Guid ApartmentId { get; init; }

    public string ApartmentName { get; init; } = string.Empty;

    public string? ApartmentImageUrl { get; init; }

    public AddressResponse Address { get; init; } = null!;

    public DateOnly DurationStart { get; init; }

    public DateOnly DurationEnd { get; init; }

    public int Nights { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal PricePerNight { get; init; }

    public decimal PriceForPeriodAmount { get; init; }

    public decimal CleaningFeeAmount { get; init; }

    public decimal AmenitiesUpChargeAmount { get; init; }

    public decimal TotalPriceAmount { get; init; }

    public BookingHostResponse Host { get; init; } = null!;

    public Guid? ConversationId { get; init; }
}

public sealed class BookingHostResponse
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }
}