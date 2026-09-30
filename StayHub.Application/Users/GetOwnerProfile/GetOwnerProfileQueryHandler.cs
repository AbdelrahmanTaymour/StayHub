using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.GetOwnerProfile;

internal sealed class GetOwnerProfileQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IFileStorageService fileStorageService)
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
                               p.avatar_key AS AvatarKey,
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

        var row = await connection.QueryFirstOrDefaultAsync<OwnerProfileRow>(sql, new { request.OwnerId });

        if (row is null)
        {
            return Result.Failure<UserProfileResponse>(UserErrors.NotFound);
        }

        return await ToUserProfileResponseAsync(row, cancellationToken);
    }

    private async Task<UserProfileResponse> ToUserProfileResponseAsync(
        OwnerProfileRow row,
        CancellationToken cancellationToken)
    {
        var avatarUrl = string.IsNullOrWhiteSpace(row.AvatarKey)
            ? null
            : await fileStorageService.GeneratePresignedUrlAsync(row.AvatarKey, cancellationToken);

        return new UserProfileResponse
        {
            Id = row.Id,
            FullName = row.FullName,
            AvatarUrl = avatarUrl,
            Bio = row.Bio,
            Rating = row.Rating,
            ReviewCount = row.ReviewCount,
            ActiveListingsCount = row.ActiveListingsCount
        };
    }

    internal sealed class OwnerProfileRow
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string? AvatarKey { get; init; }
        public string? Bio { get; init; }
        public double? Rating { get; init; }
        public int ReviewCount { get; init; }
        public int ActiveListingsCount { get; init; }
    }
}