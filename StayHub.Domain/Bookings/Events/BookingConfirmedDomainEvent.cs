using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public sealed record BookingConfirmedDomainEvent(Guid BookingId) : IDomainEvent;