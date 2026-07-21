using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments.Events;

public sealed record PaymentInitiatedDomainEvent(Guid PaymentId, Guid BookingId) : IDomainEvent;