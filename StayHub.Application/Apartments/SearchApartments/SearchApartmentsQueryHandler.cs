using System.Text;
using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Shared;

namespace StayHub.Application.Apartments.SearchApartments;

internal sealed class SearchApartmentsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    PricingService pricingService,
    IFileStorageService fileStorageService)
    : IQueryHandler<SearchApartmentsQuery, PagedResponse<SearchApartmentsResponse>>
{
    // Only a Confirmed booking blocks availability — a merely Reserved booking must not let
    // a single pending guest lock out everyone else searching for the same apartment.
    private static readonly int[] ActiveBookingStatuses =
    [
        (int)BookingStatus.Confirmed
    ];

    public async Task<Result<PagedResponse<SearchApartmentsResponse>>> Handle(
        SearchApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Start is not null &&
            request.End is not null &&
            request.Start >= request.End)
        {
            return new PagedResponse<SearchApartmentsResponse>
            {
                Items = [],
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var normalizedRequest = NormalizeRequest(request);

        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();

        var sql = new StringBuilder("""
                                    SELECT
                                        a.id AS Id,
                                        a.name AS Name,
                                        a.address_city AS City,
                                        a.address_country AS Country,
                                        a.price_amount AS PriceAmount,
                                        a.price_currency AS Currency,
                                        a.cleaning_fee_amount AS CleaningFeeAmount,
                                        a.amenities AS Amenities,
                                        img.key AS PrimaryImageKey,
                                        rv.avg_rating AS Rating,
                                        COALESCE(rv.review_count, 0) AS ReviewCount,
                                        COUNT(*) OVER() AS TotalCount

                                    FROM apartments a

                                    LEFT JOIN apartment_images img
                                        ON img.apartment_id = a.id
                                        AND img.is_primary = true

                                    LEFT JOIN LATERAL (
                                        SELECT
                                            AVG(r.rating)::float AS avg_rating,
                                            COUNT(*)::int AS review_count
                                        FROM reviews r
                                        WHERE r.apartment_id = a.id
                                    ) rv ON true

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

        var rows = (await connection.QueryAsync<ApartmentSearchRow>(sql.ToString(), parameters)).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<SearchApartmentsResponse>
            {
                Items = [],
                Page = normalizedRequest.Page,
                PageSize = normalizedRequest.PageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        // Favorites — only when authenticated, only for this page's ids, never cached
        // (the search result itself is cached and must stay user-agnostic).
        HashSet<Guid> favoritedIds = [];
        if (userContext.IsAuthenticated)
        {
            var apartmentIds = rows.Select(r => r.Id).ToArray();

            var favoriteRows = await connection.QueryAsync<Guid>(
                """
                SELECT apartment_id
                FROM favorite_apartments
                WHERE user_id = @UserId
                  AND apartment_id = ANY(@ApartmentIds)
                """,
                new { userContext.UserId, ApartmentIds = apartmentIds });

            favoritedIds = favoriteRows.ToHashSet();
        }

        int? nights = normalizedRequest is { Start: not null, End: not null }
            ? normalizedRequest.End.Value.DayNumber - normalizedRequest.Start.Value.DayNumber
            : null;

        var items = await ToSearchApartmentResponsesAsync(rows, favoritedIds, nights, cancellationToken);

        var totalCount = rows[0].TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)normalizedRequest.PageSize);

        return new PagedResponse<SearchApartmentsResponse>
        {
            Items = items,
            Page = normalizedRequest.Page,
            PageSize = normalizedRequest.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private async Task<IReadOnlyList<SearchApartmentsResponse>> ToSearchApartmentResponsesAsync(
        IReadOnlyList<ApartmentSearchRow> rows,
        HashSet<Guid> favoritedIds,
        int? nights,
        CancellationToken cancellationToken)
    {
        var tasks = rows.Select(async row =>
        {
            decimal? totalPrice = null;

            if (nights is > 0)
            {
                var amenities = (row.Amenities ?? [])
                    .Select(Enum.Parse<Amenity>)
                    .ToList();

                var pricing = pricingService.CalculatePrice(
                    new Money(row.PriceAmount, Currency.FromCode(row.Currency)),
                    new Money(row.CleaningFeeAmount, Currency.FromCode(row.Currency)),
                    amenities,
                    nights.Value);

                totalPrice = pricing.TotalPrice.Amount;
            }

            var primaryImageUrl = string.IsNullOrWhiteSpace(row.PrimaryImageKey)
                ? null
                : await fileStorageService.GeneratePresignedUrlAsync(row.PrimaryImageKey, cancellationToken);

            return new SearchApartmentsResponse
            {
                Id = row.Id,
                Name = row.Name,
                City = row.City,
                Country = row.Country,
                PricePerNight = row.PriceAmount,
                TotalPrice = totalPrice,
                Currency = row.Currency,
                PrimaryImageUrl = primaryImageUrl,
                Rating = row.Rating,
                ReviewCount = row.ReviewCount,
                IsFavorited = favoritedIds.Contains(row.Id)
            };
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    private static SearchApartmentsQuery NormalizeRequest(SearchApartmentsQuery request)
    {
        var page = request.Page < 1 ? 1 : request.Page;

        var pageSize = request.PageSize switch
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

    // Dapper projection row 
    internal sealed class ApartmentSearchRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public decimal PriceAmount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public decimal CleaningFeeAmount { get; init; }
        public IReadOnlyList<string>? Amenities { get; init; }
        public string? PrimaryImageKey { get; init; }
        public double? Rating { get; init; }
        public int ReviewCount { get; init; }
        public int TotalCount { get; init; }
    }
}