using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Apartments.GetApartmentAvailabilityBlocks;

internal sealed class GetApartmentAvailabilityBlocksQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetApartmentAvailabilityBlocksQuery, ApartmentAvailabilityResponse>
{
    private static readonly int[] BlockingStatuses = [(int)BookingStatus.Confirmed];

    public async Task<Result<ApartmentAvailabilityResponse>> Handle(
        GetApartmentAvailabilityBlocksQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        DateOnly? monthStart = null;
        DateOnly? monthEnd = null;

        if (request.Year.HasValue && request.Month.HasValue)
        {
            monthStart = new DateOnly(request.Year.Value, request.Month.Value, 1);
            monthEnd = monthStart.Value.AddMonths(1).AddDays(-1);
        }

        const string sql = """
                           SELECT a.owner_id AS OwnerId, a.is_active AS IsActive
                           FROM apartments a
                           WHERE a.id = @ApartmentId;

                           SELECT
                               aab.id AS Id,
                               aab.start AS StartDate,
                               aab.end AS EndDate,
                               aab.reason AS Reason
                           FROM apartment_availability_blocks AS aab
                           WHERE aab.apartment_id = @ApartmentId
                             AND (
                                 @MonthStart::date IS NULL
                                 OR (
                                     aab.start <= @MonthEnd::date
                                     AND aab.end >= @MonthStart::date
                                 )
                             )
                           ORDER BY aab.start ASC;

                           SELECT
                               b.id AS BookingId,
                               b.duration_start AS StartDate,
                               b.duration_end AS EndDate
                           FROM bookings b
                           WHERE b.apartment_id = @ApartmentId
                             AND b.status = ANY(@BlockingStatuses)
                             AND (
                                 @MonthStart::date IS NULL
                                 OR (
                                     b.duration_start <= @MonthEnd::date
                                     AND b.duration_end >= @MonthStart::date
                                 )
                             )
                           ORDER BY b.duration_start ASC;
                           """;

        using var multi = await connection.QueryMultipleAsync(
            sql,
            new
            {
                request.ApartmentId,
                MonthStart = monthStart,
                MonthEnd = monthEnd,
                BlockingStatuses
            });

        var apartment = (await multi.ReadAsync<ApartmentRow>()).SingleOrDefault();

        if (apartment is null)
        {
            return Result.Failure<ApartmentAvailabilityResponse>(ApartmentErrors.NotFound);
        }

        var isOwnerOrAdmin = userContext.IsOwner(apartment.OwnerId) || userContext.IsAdmin;

        // A delisted apartment's calendar is management detail, not something a stranger
        // should be able to browse just by guessing/reusing its id.
        if (!apartment.IsActive && !isOwnerOrAdmin)
        {
            return Result.Failure<ApartmentAvailabilityResponse>(ApartmentErrors.NotFound);
        }

        var blocks = await multi.ReadAsync<AvailabilityBlockRow>();
        var bookings = await multi.ReadAsync<BookedRangeResponse>();

        return new ApartmentAvailabilityResponse
        {
            Blocks = blocks
                .Select(block => new AvailabilityBlockResponse(
                    block.Id,
                    block.StartDate,
                    block.EndDate,
                    // Why a date is blocked is operational detail for the host's own
                    // dashboard, not something a public/guest caller needs to see.
                    isOwnerOrAdmin ? GetReasonDisplayName(block.Reason) : null))
                .ToList(),
            BookedRanges = bookings.ToList()
        };
    }

    private static string GetReasonDisplayName(ApartmentUnavailabilityReason reason)
    {
        return reason switch
        {
            ApartmentUnavailabilityReason.Booked => "Booked",
            ApartmentUnavailabilityReason.OwnerBlocked => "Owner Blocked",
            ApartmentUnavailabilityReason.UnderMaintenance => "Under Maintenance",
            _ => reason.ToString()
        };
    }

    private sealed record ApartmentRow(Guid OwnerId, bool IsActive);

    private sealed record AvailabilityBlockRow(
        Guid Id,
        DateOnly StartDate,
        DateOnly EndDate,
        ApartmentUnavailabilityReason Reason);
}