using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Notifications;

namespace StayHub.Application.Notifications.GetMyNotifications;

internal sealed class GetNotificationsByUserQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetMyNotificationsQuery, PagedResponse<MyNotificationsResponse>>
{
    public async Task<Result<PagedResponse<MyNotificationsResponse>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 50 => 50,
            _ => request.PageSize
        };

        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
                   SELECT
                       id AS Id,
                       type AS Type,
                       payload AS Payload,
                       is_read AS IsRead,
                       created_on_utc AS CreatedOnUtc,
                       COUNT(*) OVER() AS TotalCount
                   FROM notifications
                   WHERE user_id = @UserId
                   {(request.UnreadOnly ? "AND is_read = false" : string.Empty)}
                   ORDER BY created_on_utc DESC
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                   """;

        var rows = (await connection.QueryAsync<NotificationRow>(
            sql,
            new
            {
                userContext.UserId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            })).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<MyNotificationsResponse>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var items = rows.Select(r => new MyNotificationsResponse
        {
            Id = r.Id,
            Type = r.Type,
            Payload = r.Payload,
            IsRead = r.IsRead,
            CreatedOnUtc = r.CreatedOnUtc
        }).ToList();

        var totalCount = rows[0].TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<MyNotificationsResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private sealed class NotificationRow
    {
        public Guid Id { get; init; }
        public NotificationType Type { get; init; }
        public string Payload { get; init; } = string.Empty;
        public bool IsRead { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public int TotalCount { get; init; }
    }
}