using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Reviews.GetApartmentReviews;

internal sealed class GetApartmentReviewsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetApartmentReviewsQuery, PagedResponse<ApartmentReviewResponse>>
{
    public async Task<Result<PagedResponse<ApartmentReviewResponse>>> Handle(
        GetApartmentReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > 50 => 50,
            _ => request.PageSize
        };

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               r.id AS Id,
                               u.first_name || ' ' || u.last_name AS ReviewerName,
                               up.avatar_url AS ReviewerAvatarUrl,
                               r.rating AS Rating,
                               r.comment AS Comment,
                               rr.comment AS OwnerResponseComment,
                               r.created_on_utc AS CreatedOnUtc,
                               COUNT(*) OVER() AS TotalCount
                           FROM reviews r
                           JOIN users u ON u.id = r.user_id
                           LEFT JOIN user_profiles up ON up.user_id = u.id
                           LEFT JOIN review_responses rr ON rr.review_id = r.id
                           WHERE r.apartment_id = @ApartmentId
                           ORDER BY r.created_on_utc DESC
                           OFFSET @Offset ROWS
                           FETCH NEXT @PageSize ROWS ONLY
                           """;

        var rows = (await connection.QueryAsync<ApartmentReviewRow>(
            sql,
            new
            {
                request.ApartmentId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            })).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<ApartmentReviewResponse>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var items = rows.Select(r => new ApartmentReviewResponse
        {
            Id = r.Id,
            ReviewerName = r.ReviewerName,
            ReviewerAvatarUrl = r.ReviewerAvatarUrl,
            Rating = r.Rating,
            Comment = r.Comment,
            OwnerResponseComment = r.OwnerResponseComment,
            CreatedOnUtc = r.CreatedOnUtc
        }).ToList();

        var totalCount = rows[0].TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<ApartmentReviewResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private sealed class ApartmentReviewRow
    {
        public Guid Id { get; init; }
        public string ReviewerName { get; init; } = string.Empty;
        public string? ReviewerAvatarUrl { get; init; }
        public int Rating { get; init; }
        public string Comment { get; init; } = string.Empty;
        public string? OwnerResponseComment { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public int TotalCount { get; init; }
    }
}