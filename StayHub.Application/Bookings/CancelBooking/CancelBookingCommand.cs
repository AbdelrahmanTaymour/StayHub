using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Bookings.CancelBooking;

public sealed record CancelBookingCommand(Guid BookingId) : ICommand;