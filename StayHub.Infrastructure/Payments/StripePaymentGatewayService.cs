using Microsoft.Extensions.Options;
using StayHub.Application.Abstractions.Payments;
using StayHub.Domain.Payments;
using Stripe;

namespace StayHub.Infrastructure.Payments;

internal sealed class StripePaymentGatewayService : IPaymentGatewayService
{
    public StripePaymentGatewayService(IOptions<StripeSettings> stripeSettings)
    {
        StripeConfiguration.ApiKey = stripeSettings.Value.SecretKey;
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var service = new PaymentIntentService();

        var paymentIntent = await service.CreateAsync(
            new PaymentIntentCreateOptions
            {
                Amount = ConvertToSmallestCurrencyUnit(amount),
                Currency = currency.ToLowerInvariant(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
            },
            cancellationToken: cancellationToken);

        return new PaymentIntentResult(paymentIntent.Id, paymentIntent.ClientSecret);
    }

    public async Task RefundAsync(ProviderReference providerReference, CancellationToken cancellationToken = default)
    {
        var service = new RefundService();

        await service.CreateAsync(
            new RefundCreateOptions { PaymentIntent = providerReference.Value },
            cancellationToken: cancellationToken);
    }

    private static long ConvertToSmallestCurrencyUnit(decimal amount)
    {
        // Stripe expects amounts in the smallest unit of the currency (cents for USD/EUR).
        // This assumes 2-decimal currencies; a currency like JPY (0 decimals) would need different handling.
        return (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
    }
}