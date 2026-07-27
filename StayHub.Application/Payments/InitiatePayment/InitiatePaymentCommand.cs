using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Payments.InitiatePayment;

public sealed record InitiatePaymentCommand(Guid BookingId, Guid RequestedByUserId) : ICommand<InitiatePaymentResponse>;