using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Favorites.GetFavoriteApartments;

internal sealed class GetFavoriteApartmentsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetFavoriteApartmentsQuery, PagedResponse<FavoriteApartmentResponse>>
{
    public async Task<Result<PagedResponse<FavoriteApartmentResponse>>> Handle(
        GetFavoriteApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 12,
            > 50 => 50,
            _ => request.PageSize
        };

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               a.id AS ApartmentId,
                               a.name AS Name,
                               a.address_city AS City,
                               a.price_amount AS PricePerNight,
                               a.price_currency AS Currency,
                               img.key AS PrimaryImageKey,
                               rv.avg_rating AS Rating,
                               COALESCE(rv.review_count, 0) AS ReviewCount,
                               COUNT(*) OVER() AS TotalCount

                           FROM favorite_apartments f

                           INNER JOIN apartments a
                               ON a.id = f.apartment_id
                               AND a.is_active = true

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

                           WHERE f.user_id = @UserId

                           ORDER BY f.created_on_utc DESC
                           OFFSET @Offset ROWS
                           FETCH NEXT @PageSize ROWS ONLY
                           """;

        var rows = (await connection.QueryAsync<FavoriteApartmentRow>(
            sql,
            new
            {
                userContext.UserId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            })).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<FavoriteApartmentResponse>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var items = await ToFavoriteApartmentResponsesAsync(rows, cancellationToken);
        var totalCount = rows[0].TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<FavoriteApartmentResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private async Task<IReadOnlyList<FavoriteApartmentResponse>> ToFavoriteApartmentResponsesAsync(
        IReadOnlyList<FavoriteApartmentRow> rows,
        CancellationToken cancellationToken)
    {
        var tasks = rows.Select(async r =>
        {
            var primaryImageUrl = string.IsNullOrWhiteSpace(r.PrimaryImageKey)
                ? null
                : await fileStorageService.GeneratePresignedUrlAsync(r.PrimaryImageKey, cancellationToken);

            return new FavoriteApartmentResponse
            {
                ApartmentId = r.ApartmentId,
                Name = r.Name,
                City = r.City,
                PricePerNight = r.PricePerNight,
                Currency = r.Currency,
                PrimaryImageUrl = primaryImageUrl,
                Rating = r.Rating,
                ReviewCount = r.ReviewCount
            };
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    internal sealed class FavoriteApartmentRow
    {
        public Guid ApartmentId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public decimal PricePerNight { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string? PrimaryImageKey { get; init; }
        public double? Rating { get; init; }
        public int ReviewCount { get; init; }
        public int TotalCount { get; init; }
    }
}