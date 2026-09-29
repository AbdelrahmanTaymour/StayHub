using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Bookings.GetApartmentBookings;

public enum ApartmentBookingsFilter
{
    All,
    Pending,
    Confirmed,
    Cancelled,
    Rejected
}

public enum ApartmentBookingsSort
{
    CheckInAsc,
    CheckInDesc,
    TotalDesc,
    TotalAsc
}

public sealed record GetApartmentBookingsQuery(
    Guid ApartmentId,
    ApartmentBookingsFilter Filter = ApartmentBookingsFilter.All,
    ApartmentBookingsSort Sort = ApartmentBookingsSort.CheckInAsc,
    string? Search = null,
    int Page = 1,
    int PageSize = 10) : IQuery<PagedResponse<ApartmentBookingResponse>>;