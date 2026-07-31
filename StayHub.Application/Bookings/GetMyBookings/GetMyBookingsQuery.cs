using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Bookings.GetBookingsByUser;

namespace StayHub.Application.Bookings.GetMyBookings;

public sealed record GetMyBookingsQuery(int Page, int PageSize)
    : IQuery<IReadOnlyList<BookingSummaryResponse>>;