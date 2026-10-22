using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Notifications.GetUnreadNotificationsCount;

internal sealed class GetUnreadNotificationsCountQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetUnreadNotificationsCountQuery, int>
{
    public async Task<Result<int>> Handle(
        GetUnreadNotificationsCountQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT COUNT(*)
                           FROM notifications
                           WHERE user_id = @UserId AND is_read = false
                           """;

        var count = await connection.ExecuteScalarAsync<int>(
            sql,
            new { userContext.UserId });

        return count;
    }
}