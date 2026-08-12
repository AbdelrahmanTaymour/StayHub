using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Conversations.GetMyConversations;

internal sealed class GetMyConversationsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetMyConversationsQuery, IReadOnlyList<ConversationSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<ConversationSummaryResponse>>> Handle(
        GetMyConversationsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               c.id AS Id,
                               c.apartment_id AS ApartmentId,
                               c.guest_id AS GuestId,
                               c.owner_id AS OwnerId,
                               c.last_message_on_utc AS LastMessageOnUtc,
                               (
                                   SELECT COUNT(*)
                                   FROM messages m
                                   WHERE m.conversation_id = c.id
                                     AND m.sender_id != @UserId
                                     AND m.read_on_utc IS NULL
                               ) AS UnreadCount
                           FROM conversations c
                           WHERE c.guest_id = @UserId OR c.owner_id = @UserId
                           ORDER BY c.last_message_on_utc DESC NULLS LAST
                           """;

        var conversations = await connection.QueryAsync<ConversationSummaryResponse>(
            sql,
            new { UserId = userContext.UserId });

        return conversations.ToList();
    }
}