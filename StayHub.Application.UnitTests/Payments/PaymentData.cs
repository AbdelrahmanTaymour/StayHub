using StayHub.Domain.Payments;
using StayHub.Domain.Shared;

namespace StayHub.Application.UnitTests.Payments;

internal static class PaymentData
{
    public static readonly Money Amount = new(150m, Currency.Usd);
    public static readonly ProviderReference ProviderReference = new("pi_test_123");

    public static Payment Initiate(Guid? bookingId = null)
    {
        return Payment.Initiate(bookingId ?? Guid.CreateVersion7(), Amount, PaymentProvider.Stripe, ProviderReference,
            DateTime.UtcNow);
    }

    public static Payment InitiateAndSucceed(Guid? bookingId = null)
    {
        var payment = Initiate(bookingId);
        payment.MarkAsSucceeded(DateTime.UtcNow);
        return payment;
    }
}