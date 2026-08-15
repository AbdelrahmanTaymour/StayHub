namespace StayHub.Api.Endpoints.Payments;

public sealed record InitiatePaymentRequest(Guid BookingId);