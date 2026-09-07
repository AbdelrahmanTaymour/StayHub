using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public sealed record BookingCancelledDomainEvent(Guid BookingId) : IDomainEvent;