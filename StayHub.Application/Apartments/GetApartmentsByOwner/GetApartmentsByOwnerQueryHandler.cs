using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Apartments.GetApartmentsByOwner;

internal sealed class GetApartmentsByOwnerQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetApartmentsByOwnerQuery, PagedResponse<OwnerApartmentsResponse>>
{
    public async Task<Result<PagedResponse<OwnerApartmentsResponse>>> Handle(
        GetApartmentsByOwnerQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        var orderBy = request.Sort switch
        {
            OwnerApartmentsSort.PriceAsc => "a.price_amount ASC, a.created_on_utc DESC",
            OwnerApartmentsSort.PriceDesc => "a.price_amount DESC, a.created_on_utc DESC",
            OwnerApartmentsSort.Rating => "rv.avg_rating DESC NULLS LAST, a.created_on_utc DESC",
            OwnerApartmentsSort.Reviews => "rv.review_count DESC, a.created_on_utc DESC",
            _ => "a.price_amount ASC, a.created_on_utc DESC"
        };

        var sql = $"""
                   SELECT
                       a.id AS Id,
                       a.name AS Name,
                       a.address_city AS City,
                       a.address_country AS Country,
                       a.price_amount AS PricePerNight,
                       a.price_currency AS Currency,
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

                   WHERE a.owner_id = @OwnerId
                     AND a.is_active = true

                   ORDER BY {orderBy}

                   OFFSET @Offset ROWS
                   FETCH NEXT @PageSize ROWS ONLY
                   """;

        var rows = (await connection.QueryAsync<OwnerApartmentRow>(
            sql,
            new
            {
                request.OwnerId,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            })).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<OwnerApartmentsResponse>
            {
                Items = [],
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        // Favorites — only when authenticated, only for this page's ids, never cached.
        HashSet<Guid> favoritedIds;
        if (userContext.UserId is { } currentUserId)
        {
            var apartmentIds = rows.Select(r => r.Id).ToArray();

            var favoriteRows = await connection.QueryAsync<Guid>(
                """
                SELECT apartment_id
                FROM favorite_apartments
                WHERE user_id = @UserId
                  AND apartment_id = ANY(@ApartmentIds)
                """,
                new { UserId = currentUserId, ApartmentIds = apartmentIds });

            favoritedIds = favoriteRows.ToHashSet();
        }

        var items = await ToOwnerApartmentResponsesAsync(rows, favoritedIds, cancellationToken);

        var totalCount = rows[0].TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<OwnerApartmentsResponse>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private async Task<IReadOnlyList<OwnerApartmentsResponse>> ToOwnerApartmentResponsesAsync(
        IReadOnlyList<OwnerApartmentRow> rows,
        HashSet<Guid> favoritedIds,
        CancellationToken cancellationToken)
    {
        var tasks = rows.Select(async row =>
        {
            var primaryImageUrl = string.IsNullOrWhiteSpace(row.PrimaryImageKey)
                ? null
                : await fileStorageService.GeneratePresignedUrlAsync(row.PrimaryImageKey, cancellationToken);

            return new OwnerApartmentsResponse
            {
                Id = row.Id,
                Name = row.Name,
                City = row.City,
                Country = row.Country,
                PricePerNight = row.PricePerNight,
                Currency = row.Currency,
                PrimaryImageUrl = primaryImageUrl,
                Rating = row.Rating,
                ReviewCount = row.ReviewCount,
                IsFavorited = favoritedIds.Contains(row.Id)
            };
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    internal sealed class OwnerApartmentRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public decimal PricePerNight { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string? PrimaryImageKey { get; init; }
        public double? Rating { get; init; }
        public int ReviewCount { get; init; }
        public int TotalCount { get; init; }
    }
}