using System.Security.Cryptography;
using System.Text;

namespace StayHub.Api.FunctionalTests.Payments;

/// <summary>
/// Reproduces Stripe's own webhook-signing algorithm (documented and stable:
/// https://stripe.com/docs/webhooks/signatures#verify-manually) so tests can send genuinely
/// valid signed payloads to the real webhook endpoint.
/// Stripe.net's EventUtility.ConstructEvent verifies signatures the same way on the receiving
/// end, so a correctly computed signature here is indistinguishable from a real Stripe request.
/// </summary>
internal static class StripeWebhookSigner
{
    /// <summary>
    /// MUST match the value configured via builder.UseSetting("Stripe:WebhookSecret", ...) in
    /// FunctionalTestWebAppFactory. If that value changes, this must change too.
    /// </summary>
    public const string WebhookSecret = "whsec_test_secret_for_functional_tests";

    public static string Sign(string payload, DateTimeOffset? timestamp = null, string? secretOverride = null)
    {
        var effectiveTimestamp = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        var signedPayload = $"{effectiveTimestamp}.{payload}";
        var secret = secretOverride ?? WebhookSecret;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var hexSignature = Convert.ToHexStringLower(hash);

        return $"t={effectiveTimestamp},v1={hexSignature}";
    }
}