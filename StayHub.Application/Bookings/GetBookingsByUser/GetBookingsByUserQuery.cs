using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Bookings.GetBookingsByUser;

public sealed record GetBookingsByUserQuery(Guid UserId, int Page, int PageSize)
    : IQuery<IReadOnlyList<BookingSummaryResponse>>;