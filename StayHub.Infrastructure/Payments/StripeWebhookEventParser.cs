using Microsoft.Extensions.Options;
using Stripe;

namespace StayHub.Infrastructure.Payments;

public sealed class StripeWebhookEventParser(IOptions<StripeSettings> stripeSettings)
{
    private readonly StripeSettings _settings = stripeSettings.Value;

    /// <summary>
    ///     Verifies the request genuinely came from Stripe before returning the parsed event.
    ///     Throws StripeException if the signature is invalid - callers should return 400, not 500, in that case.
    /// </summary>
    public Event ConstructEvent(string requestBody, string stripeSignatureHeader)
    {
        return EventUtility.ConstructEvent(requestBody, stripeSignatureHeader, _settings.WebhookSecret);
    }
}