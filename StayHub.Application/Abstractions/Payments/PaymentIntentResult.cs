namespace StayHub.Application.Abstractions.Payments;

public sealed record PaymentIntentResult(string ProviderReference, string ClientSecret);