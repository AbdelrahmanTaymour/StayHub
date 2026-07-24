using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Bookings.RejectBooking;

public sealed record RejectBookingCommand(Guid BookingId, Guid RejectedByUserId) : ICommand;