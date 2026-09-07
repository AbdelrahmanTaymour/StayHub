using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public sealed record BookingReservedDomainEvent(Guid BookingId) : IDomainEvent;