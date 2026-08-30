using System.Collections.Concurrent;
using StayHub.Application.Abstractions.Payments;
using StayHub.Domain.Payments;

namespace StayHub.Application.IntegrationTests.Integration;

public sealed record CreatedPaymentIntent(decimal Amount, string Currency, string ProviderReference);

public sealed record IssuedRefund(ProviderReference ProviderReference);

/// <summary>
/// Test double for the true external boundary (Stripe). Real success path by
/// default; tests can flip <see cref="FailNextCreatePaymentIntent"/> to force
/// the failure branch of InitiatePaymentCommandHandler (e.g. to verify the
/// cancel-on-save-failure compensation) without needing a real Stripe outage.
/// </summary>
public sealed class TestPaymentGatewayService : IPaymentGatewayService
{
    private readonly ConcurrentBag<CreatedPaymentIntent> _createdPaymentIntents = new();
    private readonly ConcurrentBag<IssuedRefund> _issuedRefunds = new();

    /// <summary>
    /// When set, the next CreatePaymentIntentAsync call throws this instead of
    /// succeeding. Reset to null after it fires once, so each test controls
    /// exactly one forced failure without leaking into later tests/assertions.
    /// </summary>
    public Exception? FailNextCreatePaymentIntent { get; set; }

    public IReadOnlyCollection<CreatedPaymentIntent> CreatedPaymentIntents => _createdPaymentIntents.ToArray();

    public IReadOnlyCollection<IssuedRefund> IssuedRefunds => _issuedRefunds.ToArray();

    public Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        if (FailNextCreatePaymentIntent is { } exception)
        {
            FailNextCreatePaymentIntent = null;

            throw exception;
        }

        var providerReference = $"pi_test_{Guid.NewGuid():N}";

        _createdPaymentIntents.Add(new CreatedPaymentIntent(amount, currency, providerReference));

        return Task.FromResult(new PaymentIntentResult(providerReference, $"{providerReference}_secret"));
    }

    public Task RefundAsync(ProviderReference providerReference, CancellationToken cancellationToken = default)
    {
        _issuedRefunds.Add(new IssuedRefund(providerReference));

        return Task.CompletedTask;
    }
}