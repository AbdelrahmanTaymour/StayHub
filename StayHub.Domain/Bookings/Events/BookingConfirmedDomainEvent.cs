using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public record BookingConfirmedDomainEvent(Guid BookingId) : IDomainEvent;