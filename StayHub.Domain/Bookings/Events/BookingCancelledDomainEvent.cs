using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public record BookingCancelledDomainEvent(Guid BookingId) : IDomainEvent;