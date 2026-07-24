using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Bookings.CompleteBooking;

public sealed record CompleteBookingCommand(Guid BookingId) : ICommand;