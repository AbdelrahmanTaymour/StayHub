using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public sealed record BookingRejectedDomainEvent(Guid BookingId, Guid RejectedByUserId) : IDomainEvent;