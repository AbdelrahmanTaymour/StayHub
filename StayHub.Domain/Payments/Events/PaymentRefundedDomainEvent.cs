using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments.Events;

public record PaymentRefundedDomainEvent(Guid Id, Guid BookingId) : IDomainEvent;