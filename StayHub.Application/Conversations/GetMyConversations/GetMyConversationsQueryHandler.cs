using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Conversations.GetMyConversations;

internal sealed class GetMyConversationsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetMyConversationsQuery, IReadOnlyList<MyConversationResponse>>
{
    public async Task<Result<IReadOnlyList<MyConversationResponse>>> Handle(
        GetMyConversationsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               c.id AS Id,
                               c.apartment_id AS ApartmentId,
                               a.name AS ApartmentName,

                               CASE
                                   WHEN c.initiator_id = @UserId THEN c.recipient_id
                                   ELSE c.initiator_id
                               END AS OtherPartyId,

                               CASE
                                   WHEN c.initiator_id = @UserId THEN recipient.first_name || ' ' || recipient.last_name
                                   ELSE initiator.first_name || ' ' || initiator.last_name
                               END AS OtherPartyName,

                               CASE
                                   WHEN c.initiator_id = @UserId THEN recipient_profile.avatar_key
                                   ELSE initiator_profile.avatar_key
                               END AS OtherPartyAvatarKey,

                               lm.body AS LastMessagePreview,
                               c.last_message_on_utc AS LastMessageOnUtc,

                               (
                                   SELECT COUNT(*)
                                   FROM messages m
                                   WHERE m.conversation_id = c.id
                                     AND m.sender_id != @UserId
                                     AND m.read_on_utc IS NULL
                               ) AS UnreadCount

                           FROM conversations c

                           LEFT JOIN apartments a
                               ON a.id = c.apartment_id

                           JOIN users initiator
                               ON initiator.id = c.initiator_id

                           JOIN users recipient
                               ON recipient.id = c.recipient_id

                           LEFT JOIN user_profiles initiator_profile
                               ON initiator_profile.user_id = initiator.id

                           LEFT JOIN user_profiles recipient_profile
                               ON recipient_profile.user_id = recipient.id

                           LEFT JOIN LATERAL (
                               SELECT m.body
                               FROM messages m
                               WHERE m.conversation_id = c.id
                               ORDER BY m.sent_on_utc DESC
                               LIMIT 1
                           ) lm ON true

                           WHERE c.initiator_id = @UserId OR c.recipient_id = @UserId
                           ORDER BY c.last_message_on_utc DESC NULLS LAST
                           """;

        var rows = (await connection.QueryAsync<ConversationRow>(
            sql,
            new { userContext.UserId })).ToList();

        if (rows.Count == 0)
        {
            return Array.Empty<MyConversationResponse>();
        }

        return await ToMyConversationResponsesAsync(rows, cancellationToken);
    }

    private async Task<Result<IReadOnlyList<MyConversationResponse>>> ToMyConversationResponsesAsync(
        IReadOnlyList<ConversationRow> rows,
        CancellationToken cancellationToken)
    {
        var tasks = rows.Select(async r =>
        {
            var otherPartyAvatarUrl = string.IsNullOrWhiteSpace(r.OtherPartyAvatarKey)
                ? null
                : await fileStorageService.GeneratePresignedUrlAsync(r.OtherPartyAvatarKey, cancellationToken);

            return new MyConversationResponse
            {
                Id = r.Id,
                ApartmentId = r.ApartmentId,
                ApartmentName = r.ApartmentName,
                OtherPartyId = r.OtherPartyId,
                OtherPartyName = r.OtherPartyName,
                OtherPartyAvatarUrl = otherPartyAvatarUrl,
                LastMessagePreview = r.LastMessagePreview,
                LastMessageOnUtc = r.LastMessageOnUtc,
                UnreadCount = r.UnreadCount
            };
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    internal sealed class ConversationRow
    {
        public Guid Id { get; init; }
        public Guid? ApartmentId { get; init; }
        public string? ApartmentName { get; init; }
        public Guid OtherPartyId { get; init; }
        public string OtherPartyName { get; init; } = string.Empty;
        public string? OtherPartyAvatarKey { get; init; }
        public string? LastMessagePreview { get; init; }
        public DateTime? LastMessageOnUtc { get; init; }
        public int UnreadCount { get; init; }
    }
}