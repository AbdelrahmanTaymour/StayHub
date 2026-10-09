using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;

namespace StayHub.Application.Bookings.GetConversationBookingDetails;

public sealed record ConversationBookingDetailsResponse
{
    public Guid BookingId { get; init; }
    public BookingStatus Status { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }

    public Guid ApartmentId { get; init; }
    public string ApartmentName { get; init; } = string.Empty;
    public string? ApartmentImageUrl { get; init; }
    public string ApartmentAddress { get; init; } = string.Empty;

    public DateOnly CheckIn { get; init; }
    public DateOnly CheckOut { get; init; }
    public int Nights { get; init; }

    public decimal TotalPriceAmount { get; init; }
    public string TotalPriceCurrency { get; init; } = string.Empty;

    public string HostName { get; init; } = string.Empty;
    public string? HostAvatarUrl { get; init; }
    public string? HostPhoneNumber { get; init; }
}