using System.Text;
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

        var parameters = new DynamicParameters();
        parameters.Add("ApartmentId", request.ApartmentId);
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var where = new StringBuilder("WHERE r.apartment_id = @ApartmentId");

        switch (request.ResponseStatus)
        {
            case ReviewResponseStatusFilter.NeedsResponse:
                where.Append(" AND rr.id IS NULL");
                break;
            case ReviewResponseStatusFilter.Responded:
                where.Append(" AND rr.id IS NOT NULL");
                break;
            case ReviewResponseStatusFilter.All:
            default:
                break;
        }

        switch (request.Rating)
        {
            case ReviewRatingFilter.FiveStars:
                where.Append(" AND r.rating = 5");
                break;
            case ReviewRatingFilter.FourStars:
                where.Append(" AND r.rating = 4");
                break;
            case ReviewRatingFilter.ThreeStarsOrLess:
                where.Append(" AND r.rating <= 3");
                break;
            case ReviewRatingFilter.All:
            default:
                break;
        }

        var orderBy = request.SortOrder switch
        {
            ReviewSortOrder.Oldest => "r.created_on_utc ASC",
            ReviewSortOrder.RatingDesc => "r.rating DESC, r.created_on_utc DESC",
            ReviewSortOrder.RatingAsc => "r.rating ASC, r.created_on_utc DESC",
            ReviewSortOrder.Recent or _ => "r.created_on_utc DESC"
        };

        var sql = $"""
                   SELECT
                       r.id AS Id,
                       u.first_name || ' ' || u.last_name AS ReviewerName,
                       up.avatar_url AS ReviewerAvatarUrl,
                       b.duration_start AS BookingStartDate,
                       b.duration_end AS BookingEndDate,
                       r.rating AS Rating,
                       r.comment AS Comment,
                       rr.comment AS OwnerResponseComment,
                       rr.created_on_utc AS OwnerResponseCreatedOnUtc,
                       r.created_on_utc AS CreatedOnUtc,
                       COUNT(*) OVER() AS TotalCount
                   FROM reviews r
                   JOIN users u ON u.id = r.user_id
                   LEFT JOIN user_profiles up ON up.user_id = u.id
                   JOIN bookings b ON b.id = r.booking_id
                   LEFT JOIN review_responses rr ON rr.review_id = r.id
                   {where}
                   ORDER BY {orderBy}
                   OFFSET @Offset ROWS
                   FETCH NEXT @PageSize ROWS ONLY
                   """;

        var rows = (await connection.QueryAsync<ApartmentReviewRow>(sql, parameters)).ToList();

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
            NightsStayed = r.BookingEndDate.DayNumber - r.BookingStartDate.DayNumber,
            Rating = r.Rating,
            Comment = r.Comment,
            OwnerResponseComment = r.OwnerResponseComment,
            OwnerResponseCreatedOnUtc = r.OwnerResponseCreatedOnUtc,
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
        public DateOnly BookingStartDate { get; init; }
        public DateOnly BookingEndDate { get; init; }
        public int Rating { get; init; }
        public string Comment { get; init; } = string.Empty;
        public string? OwnerResponseComment { get; init; }
        public DateTime? OwnerResponseCreatedOnUtc { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public int TotalCount { get; init; }
    }
}