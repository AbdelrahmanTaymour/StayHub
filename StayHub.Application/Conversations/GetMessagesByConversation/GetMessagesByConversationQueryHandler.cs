using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Conversations.GetMessagesByConversation;

internal sealed class GetMessagesByConversationQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetMessagesByConversationQuery, IReadOnlyList<MessageResponse>>
{
    public async Task<Result<IReadOnlyList<MessageResponse>>> Handle(
        GetMessagesByConversationQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               id AS Id,
                               sender_id AS SenderId,
                               body AS Body,
                               sent_on_utc AS SentOnUtc,
                               read_on_utc AS ReadOnUtc
                           FROM messages
                           WHERE conversation_id = @ConversationId
                           ORDER BY sent_on_utc DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                           """;

        var messages = await connection.QueryAsync<MessageResponse>(
            sql,
            new
            {
                request.ConversationId,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return messages.ToList();
    }
}