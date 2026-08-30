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
        if (request.Start is not null &&
            request.End is not null &&
            request.Start >= request.End)
            return Array.Empty<ApartmentSummaryResponse>();

        var normalizedRequest = NormalizeRequest(request);

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
                                        ON img.apartment_id = a.id
                                        AND img.is_primary = true

                                    WHERE a.is_active = true
                                    """);


        if (!string.IsNullOrWhiteSpace(normalizedRequest.City))
        {
            sql.Append("""

                       AND a.address_city ILIKE @City
                       """);

            parameters.Add("City", $"%{normalizedRequest.City.Trim()}%");
        }


        if (normalizedRequest.MinPrice.HasValue)
        {
            sql.Append("""

                       AND a.price_amount >= @MinPrice
                       """);

            parameters.Add("MinPrice", normalizedRequest.MinPrice.Value);
        }

        if (normalizedRequest.MaxPrice.HasValue)
        {
            sql.Append("""

                       AND a.price_amount <= @MaxPrice
                       """);

            parameters.Add("MaxPrice", normalizedRequest.MaxPrice.Value);
        }


        if (normalizedRequest is { Start: not null, End: not null })
        {
            sql.Append("""

                       AND NOT EXISTS
                       (
                           SELECT 1
                           FROM bookings b
                           WHERE b.apartment_id = a.id
                             AND b.status = ANY(@ActiveBookingStatuses)
                             AND b.duration_start < @End
                             AND b.duration_end > @Start
                       )

                       AND NOT EXISTS
                       (
                           SELECT 1
                           FROM apartment_availability_blocks ab
                           WHERE ab.apartment_id = a.id
                             AND ab.start < @End
                             AND ab."end" > @Start
                       )
                       """);

            parameters.Add("ActiveBookingStatuses", ActiveBookingStatuses);
            parameters.Add("Start", normalizedRequest.Start.Value);
            parameters.Add("End", normalizedRequest.End.Value);
        }

        sql.Append("""

                   ORDER BY a.created_on_utc DESC

                   OFFSET @Offset
                   ROWS FETCH NEXT @PageSize ROWS ONLY;
                   """);

        parameters.Add("Offset", (normalizedRequest.Page - 1) * normalizedRequest.PageSize);
        parameters.Add("PageSize", normalizedRequest.PageSize);

        var apartments =
            await connection.QueryAsync<ApartmentSummaryResponse>(
                sql.ToString(),
                parameters);

        return apartments.ToList();
    }


    private static SearchApartmentsQuery NormalizeRequest(
        SearchApartmentsQuery request)
    {
        var page =
            request.Page < 1
                ? 1
                : request.Page;


        var pageSize =
            request.PageSize switch
            {
                < 1 => 10,
                > 100 => 100,
                _ => request.PageSize
            };


        return request with
        {
            Page = page,
            PageSize = pageSize,
            City = request.City?.Trim()
        };
    }
}