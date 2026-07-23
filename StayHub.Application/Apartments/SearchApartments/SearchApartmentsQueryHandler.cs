using System.Text;
using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Apartments.GetApartmentsByOwner;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Apartments.SearchApartments;

internal sealed class SearchApartmentsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<SearchApartmentsQuery, IReadOnlyList<ApartmentSummaryResponse>>
{
    private static readonly int[] ActiveBookingStatuses =
    [
        (int)BookingStatus.Reserved,
        (int)BookingStatus.Confirmed
    ];

    public async Task<Result<IReadOnlyList<ApartmentSummaryResponse>>> Handle(
        SearchApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Start is not null && request.End is not null && request.Start > request.End)
            return new List<ApartmentSummaryResponse>();

        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();

        var sql = new StringBuilder("""
                                    SELECT
                                        a.id AS Id,
                                        a.name AS Name,
                                        a.address_city AS City,
                                        a.price_amount AS Price,
                                        a.price_currency AS Currency,
                                        img.url AS PrimaryImageUrl
                                    FROM apartments a
                                    LEFT JOIN apartment_images img
                                        ON img.id = (
                                            SELECT i.id 
                                            FROM apartment_images i 
                                            WHERE i.apartment_id = a.id AND i.is_primary = true 
                                            LIMIT 1
                                        )
                                    WHERE a.is_active = true
                                    """);

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            sql.Append(" AND a.address_city ILIKE @City");
            parameters.Add("City", $"%{request.City.Trim()}%");
        }

        if (request.MinPrice is not null)
        {
            sql.Append(" AND a.price_amount >= @MinPrice");
            parameters.Add("MinPrice", request.MinPrice);
        }

        if (request.MaxPrice is not null)
        {
            sql.Append(" AND a.price_amount <= @MaxPrice");
            parameters.Add("MaxPrice", request.MaxPrice);
        }

        if (request.Start is not null && request.End is not null)
        {
            sql.Append("""

                        AND NOT EXISTS (
                            SELECT 1 FROM bookings b
                            WHERE b.apartment_id = a.id
                              AND b.status IN @ActiveBookingStatuses
                              AND b.duration_start < @End
                              AND b.duration_end > @Start
                        )
                        AND NOT EXISTS (
                            SELECT 1 FROM apartment_availability_blocks bl
                            WHERE bl.apartment_id = a.id
                              AND bl.start < @End
                              AND bl."end" > @Start
                        )
                       """);

            parameters.Add("ActiveBookingStatuses", ActiveBookingStatuses);
            parameters.Add("Start", request.Start);
            parameters.Add("End", request.End);
        }

        sql.Append(" ORDER BY a.created_on_utc DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var apartments = await connection.QueryAsync<ApartmentSummaryResponse>(sql.ToString(), parameters);

        return apartments.ToList();
    }
}