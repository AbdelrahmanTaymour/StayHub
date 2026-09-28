using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Apartments.GetMyApartments;

public class GetMyApartmentsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetMyApartmentsQuery, PagedResponse<MyApartmentsResponse>>
{
    private const int WindowDays = 30;

    private static readonly int[] OccupyingStatuses =
    [
        (int)BookingStatus.Confirmed,
        (int)BookingStatus.Completed
    ];

    public async Task<Result<PagedResponse<MyApartmentsResponse>>> Handle(
        GetMyApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch { < 1 => 10, > 50 => 50, _ => request.PageSize };

        var hasSearch = !string.IsNullOrWhiteSpace(request.Search);

        // Closed enum / fixed literals only
        var statusFilter = request.Status switch
        {
            MyApartmentsFilter.Active => "AND a.is_active = true",
            MyApartmentsFilter.Inactive => "AND a.is_active = false",
            _ => string.Empty
        };

        var searchFilter = hasSearch
            ? "AND (a.name ILIKE @Search OR a.address_city ILIKE @Search OR a.id::text ILIKE @Search)"
            : string.Empty;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var sql = $"""
                   WITH page AS (
                       SELECT
                           a.id,
                           a.name,
                           a.address_city,
                           a.address_country,
                           a.is_active,
                           a.price_amount,
                           a.price_currency,
                           a.created_on_utc,
                           COUNT(*) OVER() AS total_count
                       FROM apartments a
                       WHERE a.owner_id = @OwnerId
                       {statusFilter}
                       {searchFilter}
                       ORDER BY a.created_on_utc DESC
                       OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                   )
                   SELECT
                       p.id AS Id,
                       p.name AS Name,
                       p.address_city AS City,
                       p.address_country AS Country,
                       p.is_active AS IsActive,
                       p.price_amount AS PricePerNight,
                       p.price_currency AS Currency,
                       img.url AS PrimaryImageUrl,
                       COALESCE(ph.photo_count, 0) AS PhotoCount,
                       rv.avg_rating AS Rating,
                       COALESCE(rv.review_count, 0) AS ReviewCount,
                       COALESCE(bk.pending_count, 0) AS PendingBookingsCount,
                       COALESCE(bk.occupied_nights, 0) AS OccupiedNightsLast30Days,
                       p.total_count AS TotalCount

                   FROM page p

                   LEFT JOIN apartment_images img
                       ON img.apartment_id = p.id
                       AND img.is_primary = true

                   LEFT JOIN LATERAL (
                       SELECT COUNT(*)::int AS photo_count
                       FROM apartment_images i
                       WHERE i.apartment_id = p.id
                   ) ph ON true

                   LEFT JOIN LATERAL (
                       SELECT
                           AVG(r.rating)::float AS avg_rating,
                           COUNT(*)::int AS review_count
                       FROM reviews r
                       WHERE r.apartment_id = p.id
                   ) rv ON true

                   LEFT JOIN LATERAL (
                       SELECT
                           COUNT(*) FILTER (
                               WHERE b.status = @Reserved
                                 AND b.duration_start >= @WindowEnd
                           )::int AS pending_count,
                           COALESCE(SUM(GREATEST(0,
                               LEAST(b.duration_end, @WindowEnd) - GREATEST(b.duration_start, @WindowStart)))
                               FILTER (WHERE b.status = ANY(@OccupyingStatuses)), 0)::int AS occupied_nights
                       FROM bookings b
                       WHERE b.apartment_id = p.id
                         AND b.duration_end > @WindowStart
                   ) bk ON true

                   ORDER BY p.created_on_utc DESC
                   """;

        using var connection = sqlConnectionFactory.CreateConnection();

        var rows = (await connection.QueryAsync<MyApartmentRow>(
            sql,
            new
            {
                OwnerId = userContext.UserId,
                Search = hasSearch ? $"%{EscapeLike(request.Search!.Trim())}%" : null,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize,
                Reserved = (int)BookingStatus.Reserved,
                OccupyingStatuses,
                WindowEnd = today,
                WindowStart = today.AddDays(-WindowDays)
            })).ToList();

        return BuildPage(rows, page, pageSize);
    }

    private static PagedResponse<MyApartmentsResponse> BuildPage(
        List<MyApartmentRow> rows, int page, int pageSize)
    {
        if (rows.Count == 0)
        {
            return new PagedResponse<MyApartmentsResponse>
            {
                Items = [], Page = page, PageSize = pageSize, TotalCount = 0, TotalPages = 0
            };
        }

        var items = rows.Select(r => new MyApartmentsResponse
        {
            Id = r.Id,
            Name = r.Name,
            City = r.City,
            Country = r.Country,
            PrimaryImageUrl = r.PrimaryImageUrl,
            IsActive = r.IsActive,
            PricePerNight = r.PricePerNight,
            Currency = r.Currency,
            Rating = r.Rating,
            ReviewCount = r.ReviewCount,
            OccupancyRateLast30Days = Math.Min(100, Math.Round(r.OccupiedNightsLast30Days * 100d / WindowDays, 1)),
            PhotoCount = r.PhotoCount,
            PendingBookingsCount = r.PendingBookingsCount
        }).ToList();

        var totalCount = rows[0].TotalCount;

        return new PagedResponse<MyApartmentsResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    // ILIKE treats % and _ as wildcards. Escape them so a search for "50%_off" is literal.
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private sealed class MyApartmentRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public string? PrimaryImageUrl { get; init; }
        public bool IsActive { get; init; }
        public decimal PricePerNight { get; init; }
        public string Currency { get; init; } = string.Empty;
        public double? Rating { get; init; }
        public int ReviewCount { get; init; }
        public int OccupiedNightsLast30Days { get; init; }
        public int PhotoCount { get; init; }
        public int PendingBookingsCount { get; init; }
        public int TotalCount { get; init; }
    }
}