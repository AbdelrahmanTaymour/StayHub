using Dapper;
using StayHub.Application.Abstractions.Data;

namespace StayHub.Infrastructure.Authorization;

internal sealed class AuthorizationService(ISqlConnectionFactory sqlConnectionFactory)
{
    public async Task<UserRolesResponse?> GetRolesForUserAsync(string identityId)
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


    public async Task<HashSet<string>> GetPermissionsForUserAsync(string identityId)
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
}