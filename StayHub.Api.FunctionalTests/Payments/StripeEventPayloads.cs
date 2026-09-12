using Stripe;

namespace StayHub.Api.FunctionalTests.Payments;

internal static class StripeEventPayloads
{
    public static string PaymentIntentSucceeded(string providerReference)
    {
        return $$"""
                 {
                   "id": "evt_test_{{Guid.NewGuid():N}}",
                   "object": "event",
                   "api_version": "{{StripeConfiguration.ApiVersion}}",
                   "type": "payment_intent.succeeded",
                   "data": {
                     "object": {
                       "id": "{{providerReference}}",
                       "object": "payment_intent",
                       "status": "succeeded",
                       "amount": 10000,
                       "currency": "usd"
                     }
                   }
                 }
                 """;
    }

    public static string PaymentIntentFailed(string providerReference)
    {
        return $$"""
                 {
                   "id": "evt_test_{{Guid.NewGuid():N}}",
                   "object": "event",
                   "api_version": "{{StripeConfiguration.ApiVersion}}",
                   "type": "payment_intent.payment_failed",
                   "data": {
                     "object": {
                       "id": "{{providerReference}}",
                       "object": "payment_intent",
                       "status": "requires_payment_method",
                       "amount": 10000,
                       "currency": "usd"
                     }
                   }
                 }
                 """;
    }

    /// <summary>An event type PaymentEndpoints.Webhook's switch statement doesn't handle at all.</summary>
    public static string UnhandledEventType(string providerReference)
    {
        return $$"""
                 {
                   "id": "evt_test_{{Guid.NewGuid():N}}",
                   "object": "event",
                   "api_version": "{{StripeConfiguration.ApiVersion}}",
                   "type": "payment_intent.created",
                   "data": {
                     "object": {
                       "id": "{{providerReference}}",
                       "object": "payment_intent",
                       "status": "requires_payment_method",
                       "amount": 10000,
                       "currency": "usd"
                     }
                   }
                 }
                 """;
    }
}