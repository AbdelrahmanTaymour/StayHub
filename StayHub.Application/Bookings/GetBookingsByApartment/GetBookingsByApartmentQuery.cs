using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Bookings.GetBookingsByUser;

namespace StayHub.Application.Bookings.GetBookingsByApartment;

public sealed record GetBookingsByApartmentQuery(Guid ApartmentId, int Page, int PageSize)
    : IQuery<IReadOnlyList<BookingSummaryResponse>>;