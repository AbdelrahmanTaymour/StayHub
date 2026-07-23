using StayHub.Domain.Payments;

namespace StayHub.Application.Abstractions.Payments;

public interface IPaymentGatewayService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);

    Task RefundAsync(ProviderReference providerReference, CancellationToken cancellationToken = default);
}