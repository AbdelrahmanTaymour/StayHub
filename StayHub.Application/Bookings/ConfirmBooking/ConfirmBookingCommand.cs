using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Bookings.ConfirmBooking;

public sealed record ConfirmBookingCommand(Guid BookingId) : ICommand;