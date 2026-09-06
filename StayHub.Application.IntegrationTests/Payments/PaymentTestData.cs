using StayHub.Domain.Payments;
using StayHub.Domain.Shared;

namespace StayHub.Application.IntegrationTests.Payments;

internal static class PaymentTestData
{
    public static Payment Initiate(
        Guid bookingId,
        decimal amount,
        string currency,
        string providerReference,
        DateTime? utcNow = null)
    {
        return Payment.Initiate(
            bookingId,
            new Money(amount, Currency.FromCode(currency)),
            PaymentProvider.Stripe,
            new ProviderReference(providerReference),
            utcNow ?? DateTime.UtcNow);
    }
}