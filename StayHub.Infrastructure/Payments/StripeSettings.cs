namespace StayHub.Infrastructure.Payments;

public sealed class StripeSettings
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; init; }

    public string WebhookSecret { get; init; }
}