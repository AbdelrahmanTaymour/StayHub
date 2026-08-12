namespace StayHub.Api.Controllers.Payments;

public sealed record InitiatePaymentRequest(Guid BookingId);