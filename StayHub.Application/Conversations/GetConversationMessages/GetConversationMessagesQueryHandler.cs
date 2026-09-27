using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Conversations.GetConversationMessages;

internal sealed class GetConversationMessagesQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetConversationMessagesQuery, PagedResponse<ConversationMessagesResponse>>
{
    public async Task<Result<PagedResponse<ConversationMessagesResponse>>> Handle(
        GetConversationMessagesQuery request,
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

        // Security check inside SQL query: verify user is participant
        const string sql = """
                           SELECT
                               m.id AS Id,
                               m.sender_id AS SenderId,
                               m.body AS Body,
                               m.sent_on_utc AS SentOnUtc,
                               m.read_on_utc AS ReadOnUtc,
                               COUNT(*) OVER() AS TotalCount
                           FROM messages m
                           INNER JOIN conversations c ON c.id = m.conversation_id
                           WHERE m.conversation_id = @ConversationId
                             AND (c.guest_id = @UserId OR c.owner_id = @UserId)
                           ORDER BY m.sent_on_utc DESC
                           LIMIT @PageSize OFFSET @Offset
                           """;

        var rows = (await connection.QueryAsync<MessageRow>(
            sql,
            new
            {
                request.ConversationId,
                userContext.UserId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            })).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<ConversationMessagesResponse>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var items = rows.Select(r => new ConversationMessagesResponse
        {
            Id = r.Id,
            SenderId = r.SenderId,
            Body = r.Body,
            SentOnUtc = r.SentOnUtc,
            ReadOnUtc = r.ReadOnUtc
        }).ToList();

        var totalCount = rows[0].TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<ConversationMessagesResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private sealed class MessageRow
    {
        public Guid Id { get; init; }
        public Guid SenderId { get; init; }
        public string Body { get; init; } = string.Empty;
        public DateTime SentOnUtc { get; init; }
        public DateTime? ReadOnUtc { get; init; }
        public int TotalCount { get; init; }
    }
}