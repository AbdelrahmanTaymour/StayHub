using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments.Events;

public sealed record PaymentSucceededDomainEvent(Guid PaymentId, Guid BookingId) : IDomainEvent;