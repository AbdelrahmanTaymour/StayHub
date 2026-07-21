using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments.Events;

public record PaymentFailedDomainEvent(Guid Id, Guid BookingId) : IDomainEvent;