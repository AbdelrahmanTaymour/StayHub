using Dapper;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Abstractions.Data;

namespace StayHub.Infrastructure.Authorization;

internal sealed class AuthorizationService(ISqlConnectionFactory sqlConnectionFactory, ICacheService cacheService)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public Task<UserRolesResponse?> GetRolesForUserAsync(string identityId,
        CancellationToken cancellationToken = default)
    {
        return cacheService.GetOrCreateAsync(
            RolesCacheKey(identityId),
            _ => LoadRolesAsync(identityId),
            CacheDuration,
            cancellationToken);
    }

    public Task<HashSet<string>> GetPermissionsForUserAsync(string identityId,
        CancellationToken cancellationToken = default)
    {
        return cacheService.GetOrCreateAsync(
            PermissionsCacheKey(identityId),
            _ => LoadPermissionsAsync(identityId),
            CacheDuration,
            cancellationToken);
    }

    public async Task InvalidateAsync(string identityId, CancellationToken cancellationToken = default)
    {
        //TODO: Call this the moment a user's role assignments actually change (once that feature exists) -
        // without it, a role change wouldn't be reflected until both cache entries naturally expire.
        await cacheService.RemoveAsync(RolesCacheKey(identityId), cancellationToken);
        await cacheService.RemoveAsync(PermissionsCacheKey(identityId), cancellationToken);
    }


    private async Task<UserRolesResponse?> LoadRolesAsync(string identityId)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               u.id AS Id,
                               r.id AS RoleId,
                               r.name AS Name
                           FROM users u
                           INNER JOIN user_roles ur ON u.id = ur.user_id
                           INNER JOIN roles r ON r.id = ur.role_id
                           WHERE u.identity_id = @IdentityId;
                           """;

        UserRolesResponse? userRolesResponse = null;

        await connection.QueryAsync<UserRolesResponse, RoleResponse, UserRolesResponse>(
            sql,
            (user, role) =>
            {
                userRolesResponse ??= user;
                userRolesResponse.Roles.Add(role);
                return userRolesResponse;
            },
            new { IdentityId = identityId },
            splitOn: "RoleId");

        return userRolesResponse;
    }


    private async Task<HashSet<string>> LoadPermissionsAsync(string identityId)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT DISTINCT p.name
                           FROM users u
                           INNER JOIN user_roles ur ON ur.user_id = u.id
                           INNER JOIN role_permissions rp ON rp.role_id = ur.role_id
                           INNER JOIN permissions p ON p.id = rp.permission_id
                           WHERE u.identity_id = @IdentityId
                           """;

        var permissions = await connection.QueryAsync<string>(sql, new { identityId });

        return permissions.ToHashSet();
    }

    private static string RolesCacheKey(string identityId)
    {
        return $"roles:{identityId}";
    }

    private static string PermissionsCacheKey(string identityId)
    {
        return $"permissions:{identityId}";
    }
}