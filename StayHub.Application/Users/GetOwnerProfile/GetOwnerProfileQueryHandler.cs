using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.GetOwnerProfile;

internal sealed class GetOwnerProfileQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetOwnerProfileQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        GetOwnerProfileQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               u.id AS Id,
                               u.first_name || ' ' || u.last_name AS FullName,
                               p.avatar_url AS AvatarUrl,
                               p.bio AS Bio,
                               stats.rating AS Rating,
                               COALESCE(stats.review_count, 0) AS ReviewCount,
                               COALESCE(stats.active_listings_count, 0) AS ActiveListingsCount
                           FROM users u
                           LEFT JOIN user_profiles p
                               ON p.user_id = u.id
                           LEFT JOIN LATERAL (
                               SELECT
                                   AVG(r.rating)::float AS rating,
                                   COUNT(r.id)::int AS review_count,
                                   COUNT(DISTINCT a.id) FILTER (WHERE a.is_active = true)::int AS active_listings_count
                               FROM apartments a
                               LEFT JOIN reviews r ON r.apartment_id = a.id
                               WHERE a.owner_id = u.id
                           ) stats ON true
                           WHERE u.id = @OwnerId
                           """;

        var profile = await connection.QueryFirstOrDefaultAsync<UserProfileResponse>(sql, new { request.OwnerId });

        return profile ?? Result.Failure<UserProfileResponse>(UserErrors.NotFound);
    }
}