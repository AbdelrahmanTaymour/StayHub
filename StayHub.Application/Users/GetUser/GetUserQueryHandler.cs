using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.GetUser;

internal sealed class GetUserQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    ICacheService cacheService) : IQueryHandler<GetUserQuery, UserResponse>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<Result<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId != request.UserId &&
            !userContext.Roles.Contains(Role.Admin.Name))
        {
            return Result.Failure<UserResponse>(UserErrors.NotAuthorized);
        }

        var user = await cacheService.GetOrCreateAsync(
            CacheKeys.User(request.UserId),
            _ => LoadAsync(request.UserId),
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