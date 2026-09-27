using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Bookings.GetMyBookings;

public sealed record GetMyBookingsQuery(
    MyBookingsFilter Filter = MyBookingsFilter.All,
    int Page = 1,
    int PageSize = 10) : IQuery<PagedResponse<MyBookingsResponse>>;