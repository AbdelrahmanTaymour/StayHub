using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Conversations.GetConversationMessages;

public sealed record GetConversationMessagesQuery(
    Guid ConversationId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResponse<ConversationMessagesResponse>>;