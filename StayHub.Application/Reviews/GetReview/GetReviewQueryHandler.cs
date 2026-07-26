using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Reviews;

namespace StayHub.Application.Reviews.GetReview;

internal sealed class GetReviewQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory) : IQueryHandler<GetReviewQuery, ReviewResponse>
{
    public async Task<Result<ReviewResponse>> Handle(GetReviewQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               r.id AS Id,
                               r.apartment_id AS ApartmentId,
                               r.booking_id AS BookingId,
                               r.user_id AS UserId,
                               r.rating AS Rating,
                               r.comment AS Comment,
                               r.created_on_utc AS CreatedOnUtc,
                               rr.comment AS OwnerResponseComment
                           FROM reviews r
                           LEFT JOIN review_responses rr ON rr.review_id = r.id
                           WHERE r.id = @ReviewId
                           """;

        var review = await connection.QueryFirstOrDefaultAsync<ReviewResponse>(sql, new { request.ReviewId });

        return review is null
            ? Result.Failure<ReviewResponse>(ReviewErrors.NotFound)
            : Result.Success(review);
    }
}