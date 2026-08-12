using Dapper;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Reviews.GetReviewsByApartment;

internal sealed class GetReviewsByApartmentQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    ICacheService cacheService)
    : IQueryHandler<GetReviewsByApartmentQuery, IReadOnlyList<ReviewListItemResponse>>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(3);

    public async Task<Result<IReadOnlyList<ReviewListItemResponse>>> Handle(
        GetReviewsByApartmentQuery request,
        CancellationToken cancellationToken)
    {
        return await cacheService.GetOrCreateAsync(
            CacheKeys.ReviewsByApartment(request.ApartmentId, request.Page, request.PageSize),
            _ => LoadAsync(request),
            CacheDuration,
            cancellationToken);
    }

    private async Task<Result<IReadOnlyList<ReviewListItemResponse>>> LoadAsync(GetReviewsByApartmentQuery request)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               r.id AS Id,
                               r.user_id AS UserId,
                               r.rating AS Rating,
                               r.comment AS Comment,
                               r.created_on_utc AS CreatedOnUtc,
                               rr.comment AS OwnerResponseComment
                           FROM reviews r
                           LEFT JOIN review_responses rr ON rr.review_id = r.id
                           WHERE r.apartment_id = @ApartmentId
                           ORDER BY r.created_on_utc DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                           """;

        var reviews = await connection.QueryAsync<ReviewListItemResponse>(
            sql,
            new
            {
                request.ApartmentId,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return reviews.ToList();
    }
}