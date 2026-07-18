using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public record BookingCompletedDomainEvent(Guid BookingId) : IDomainEvent;