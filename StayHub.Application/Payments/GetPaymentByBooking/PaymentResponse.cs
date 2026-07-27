namespace StayHub.Application.Payments.GetPaymentByBooking;

public sealed class PaymentResponse
{
    public Guid Id { get; init; }

    public Guid BookingId { get; init; }

    public decimal AmountValue { get; init; }

    public string AmountCurrency { get; init; }

    public int Status { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? ProcessedOnUtc { get; init; }
}