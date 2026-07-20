using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings.Events;

public record BookingRejectedDomainEvent(Guid BookingId, Guid RejectedByUserId) : IDomainEvent;