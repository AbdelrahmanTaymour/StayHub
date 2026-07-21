using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments.Events;

public sealed record PaymentRefundedDomainEvent(Guid PaymentId, Guid BookingId) : IDomainEvent;