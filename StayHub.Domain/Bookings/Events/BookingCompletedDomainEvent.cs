using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public sealed record BookingCompletedDomainEvent(Guid BookingId) : IDomainEvent;