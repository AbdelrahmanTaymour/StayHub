using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Notifications.GetNotificationsByUser;

internal sealed class GetNotificationsByUserQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetNotificationsByUserQuery, IReadOnlyList<NotificationResponse>>
{
    public async Task<Result<IReadOnlyList<NotificationResponse>>> Handle(
        GetNotificationsByUserQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
                   SELECT
                       id AS Id,
                       type AS Type,
                       payload AS Payload,
                       is_read AS IsRead,
                       created_on_utc AS CreatedOnUtc
                   FROM notifications
                   WHERE user_id = @UserId
                   {(request.UnreadOnly ? "AND is_read = false" : string.Empty)}
                   ORDER BY created_on_utc DESC
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                   """;

        var notifications = await connection.QueryAsync<NotificationResponse>(
            sql,
            new
            {
                request.UserId,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return notifications.ToList();
    }
}