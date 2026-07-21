using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments.Events;

public sealed record PaymentFailedDomainEvent(Guid PaymentId, Guid BookingId) : IDomainEvent;