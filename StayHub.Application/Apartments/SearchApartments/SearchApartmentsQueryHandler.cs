using System.Text;
using Dapper;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Apartments.GetApartmentsByOwner;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Apartments.SearchApartments;

internal sealed class SearchApartmentsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    ICacheService cacheService)
    : IQueryHandler<SearchApartmentsQuery, IReadOnlyList<ApartmentSummaryResponse>>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(45);

    private static readonly int[] ActiveBookingStatuses =
    [
        (int)BookingStatus.Reserved,
        (int)BookingStatus.Confirmed
    ];


    public async Task<Result<IReadOnlyList<ApartmentSummaryResponse>>> Handle(
        SearchApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = NormalizeRequest(request);

        var cacheKey = CacheKeys.ApartmentSearch(
            BuildFilterKey(normalizedRequest));


        var apartments = await cacheService.GetOrCreateAsync(
            cacheKey,
            _ => LoadApartmentsAsync(normalizedRequest),
            CacheDuration,
            cancellationToken);


        return apartments.ToList();
    }


    private async Task<IReadOnlyList<ApartmentSummaryResponse>> LoadApartmentsAsync(
        SearchApartmentsQuery request)
    {
        if (request.Start is not null &&
            request.End is not null &&
            request.Start >= request.End)
            return Array.Empty<ApartmentSummaryResponse>();

        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();

        var sql = new StringBuilder("""
                                    SELECT
                                        a.id AS Id,
                                        a.name AS Name,
                                        a.address_city AS City,
                                        a.price_amount AS PriceAmount,
                                        a.price_currency AS PriceCurrency,
                                        img.url AS PrimaryImageUrl
                                    FROM apartments a

                                    LEFT JOIN apartment_images img
                                        ON img.apartment_id = a.id
                                        AND img.is_primary = true

                                    WHERE a.is_active = true
                                    """);


        if (!string.IsNullOrWhiteSpace(request.City))
        {
            sql.Append("""

                       AND a.address_city ILIKE @City
                       """);

            parameters.Add(
                "City",
                $"%{request.City.Trim()}%");
        }


        if (request.MinPrice.HasValue)
        {
            sql.Append("""

                       AND a.price_amount >= @MinPrice
                       """);

            parameters.Add(
                "MinPrice",
                request.MinPrice.Value);
        }


        if (request.MaxPrice.HasValue)
        {
            sql.Append("""

                       AND a.price_amount <= @MaxPrice
                       """);

            parameters.Add(
                "MaxPrice",
                request.MaxPrice.Value);
        }


        if (request.Start.HasValue &&
            request.End.HasValue)
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


            parameters.Add(
                "ActiveBookingStatuses",
                ActiveBookingStatuses);

            parameters.Add(
                "Start",
                request.Start.Value);

            parameters.Add(
                "End",
                request.End.Value);
        }


        sql.Append("""

                   ORDER BY a.created_on_utc DESC

                   OFFSET @Offset
                   ROWS FETCH NEXT @PageSize ROWS ONLY;
                   """);


        parameters.Add(
            "Offset",
            (request.Page - 1) * request.PageSize);


        parameters.Add(
            "PageSize",
            request.PageSize);


        var apartments =
            await connection.QueryAsync<ApartmentSummaryResponse>(
                sql.ToString(),
                parameters);


        return apartments.AsList();
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


    private static string BuildFilterKey(
        SearchApartmentsQuery request)
    {
        return string.Join(
            '|',
            request.City,
            request.MinPrice,
            request.MaxPrice,
            request.Start,
            request.End,
            request.Page,
            request.PageSize);
    }
}