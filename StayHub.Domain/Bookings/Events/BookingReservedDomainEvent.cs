using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public record BookingReservedDomainEvent(Guid BookingId) : IDomainEvent;