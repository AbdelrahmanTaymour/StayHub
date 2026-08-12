using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Conversations.GetMessagesByConversation;

internal sealed class GetMessagesByConversationQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetMessagesByConversationQuery, IReadOnlyList<MessageResponse>>
{
    public async Task<Result<IReadOnlyList<MessageResponse>>> Handle(
        GetMessagesByConversationQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        // Security check inside SQL query: verify user is participant
        const string sql = """
                           SELECT
                               m.id AS Id,
                               m.sender_id AS SenderId,
                               m.body AS Body,
                               m.sent_on_utc AS SentOnUtc,
                               m.read_on_utc AS ReadOnUtc
                           FROM messages m
                           INNER JOIN conversations c ON c.id = m.conversation_id
                           WHERE m.conversation_id = @ConversationId
                             AND (c.guest_id = @UserId OR c.owner_id = @UserId)
                           ORDER BY m.sent_on_utc DESC
                           LIMIT @PageSize OFFSET @Offset
                           """;

        var messages = await connection.QueryAsync<MessageResponse>(
            sql,
            new
            {
                request.ConversationId,
                UserId = userContext.UserId,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return messages.ToList();
    }
}