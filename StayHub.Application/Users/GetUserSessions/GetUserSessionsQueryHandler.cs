using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.GetUserSessions;

internal sealed class GetUserSessionsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetUserSessionsQuery, IReadOnlyList<UserSessionResponse>>
{
    public async Task<Result<IReadOnlyList<UserSessionResponse>>> Handle(
        GetUserSessionsQuery request,
        CancellationToken cancellationToken)
    {
        if (userContext.UserId != request.UserId &&
            !userContext.Roles.Contains(Role.Admin.Name))
        {
            return Result.Failure<IReadOnlyList<UserSessionResponse>>(UserSessionErrors.NotAuthorized);
        }

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               id AS Id,
                               device_info AS DeviceInfo,
                               ip_address AS IpAddress,
                               created_on_utc AS CreatedOnUtc,
                               last_seen_on_utc AS LastSeenOnUtc
                           FROM user_sessions
                           WHERE user_id = @UserId AND revoked_on_utc IS NULL
                           ORDER BY last_seen_on_utc DESC
                           """;

        var sessions = await connection.QueryAsync<UserSessionResponse>(sql, new { request.UserId });

        return sessions.ToList();
    }
}