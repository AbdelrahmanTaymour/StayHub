using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments.Events;

public record PaymentInitiatedDomainEvent(Guid Id, Guid BookingId) : IDomainEvent;