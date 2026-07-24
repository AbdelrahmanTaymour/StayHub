using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Bookings.GetBooking;

public sealed record GetBookingQuery(Guid BookingId) : IQuery<BookingResponse>;