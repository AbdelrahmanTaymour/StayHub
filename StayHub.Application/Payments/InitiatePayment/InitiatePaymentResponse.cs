namespace StayHub.Application.Payments.InitiatePayment;

public sealed record InitiatePaymentResponse(Guid PaymentId, string ClientSecret);