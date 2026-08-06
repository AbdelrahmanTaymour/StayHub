using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Payments.RefundPayment;

public sealed record RefundPaymentCommand(Guid PaymentId) : ICommand;