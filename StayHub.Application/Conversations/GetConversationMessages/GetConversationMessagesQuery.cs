using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.GetConversationMessages;

public sealed record GetConversationMessagesQuery(Guid ConversationId, int Page, int PageSize)
    : IQuery<IReadOnlyList<ConversationMessagesResponse>>;