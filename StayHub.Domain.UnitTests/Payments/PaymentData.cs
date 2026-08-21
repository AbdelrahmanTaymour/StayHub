using StayHub.Domain.Payments;
using StayHub.Domain.Shared;

namespace StayHub.Domain.UnitTests.Payments;

internal static class PaymentData
{
    public static readonly Money Amount = new(150m, Currency.Usd);
    public static readonly ProviderReference ProviderReference = new("pi_test_123");

    public static Payment Initiate(Guid? bookingId = null, PaymentProvider provider = PaymentProvider.Stripe)
    {
        return Payment.Initiate(
            bookingId ?? Guid.CreateVersion7(),
            Amount,
            provider,
            ProviderReference,
            DateTime.UtcNow);
    }

    public static Payment InitiateAndSucceed(Guid? bookingId = null)
    {
        var payment = Initiate(bookingId);
        payment.MarkAsSucceeded(DateTime.UtcNow);
        return payment;
    }

    public static Payment InitiateAndFail(Guid? bookingId = null)
    {
        var payment = Initiate(bookingId);
        payment.MarkAsFailed(DateTime.UtcNow);
        return payment;
    }
}