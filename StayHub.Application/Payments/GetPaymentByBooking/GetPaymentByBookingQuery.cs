using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Payments.GetPaymentByBooking;

public sealed record GetPaymentByBookingQuery(Guid BookingId) : IQuery<PaymentResponse>;