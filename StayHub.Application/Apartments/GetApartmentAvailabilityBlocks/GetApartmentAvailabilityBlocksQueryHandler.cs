using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartmentAvailabilityBlocks;

internal sealed class GetApartmentAvailabilityBlocksQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetApartmentAvailabilityBlocksQuery, ApartmentAvailabilityResponse>
{
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
                           SELECT EXISTS (
                               SELECT 1
                               FROM apartments
                               WHERE id = @ApartmentId
                           );

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
                           """;

        using var multi = await connection.QueryMultipleAsync(
            sql,
            new
            {
                request.ApartmentId,
                MonthStart = monthStart,
                MonthEnd = monthEnd
            });

        var apartmentExists = await multi.ReadSingleAsync<bool>();

        if (!apartmentExists)
            return Result.Failure<ApartmentAvailabilityResponse>(ApartmentErrors.NotFound);

        var blocks = await multi.ReadAsync<AvailabilityBlockRow>();

        return new ApartmentAvailabilityResponse
        {
            Blocks = blocks
                .Select(block => new AvailabilityBlockResponse(
                    block.Id,
                    block.StartDate,
                    block.EndDate,
                    GetReasonDisplayName(block.Reason)))
                .ToList()
        };
    }

    private static string GetReasonDisplayName(
        ApartmentUnavailabilityReason reason)
    {
        return reason switch
        {
            ApartmentUnavailabilityReason.Booked => "Booked",
            ApartmentUnavailabilityReason.OwnerBlocked => "Owner Blocked",
            ApartmentUnavailabilityReason.UnderMaintenance => "Under Maintenance",
            _ => reason.ToString()
        };
    }

    private sealed record AvailabilityBlockRow(
        Guid Id,
        DateOnly StartDate,
        DateOnly EndDate,
        ApartmentUnavailabilityReason Reason);
}