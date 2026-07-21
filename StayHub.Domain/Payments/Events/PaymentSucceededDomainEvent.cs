using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments.Events;

public record PaymentSucceededDomainEvent(Guid Id, Guid BookingId) : IDomainEvent;