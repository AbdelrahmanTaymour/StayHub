using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Users.GetUser;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.GetLoggedInUser;

internal sealed class GetLoggedInUserHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    ICacheService cacheService) : IQueryHandler<GetLoggedInUserQuery, UserResponse>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<Result<UserResponse>> Handle(GetLoggedInUserQuery request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;

        var user = await cacheService.GetOrCreateAsync(
            CacheKeys.LoggedInUser(userId),
            _ => LoadAsync(userId),
            CacheDuration,
            cancellationToken);

        return user ?? Result.Failure<UserResponse>(UserErrors.NotFound);
    }

    private async Task<UserResponse?> LoadAsync(Guid userId)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               u.id AS Id,
                               u.first_name AS FirstName,
                               u.last_name AS LastName,
                               u.email AS Email,
                               p.avatar_url AS AvatarUrl,
                               p.bio AS Bio,
                               p.phone_number AS PhoneNumber
                           FROM users u
                           LEFT JOIN user_profiles p ON p.user_id = u.id
                           WHERE u.id = @UserId
                           """;

        return await connection.QueryFirstOrDefaultAsync<UserResponse>(sql, new { UserId = userId });
    }
}