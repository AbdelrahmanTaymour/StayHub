using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.GetMessagesByConversation;

public sealed record GetMessagesByConversationQuery(Guid ConversationId, int Page, int PageSize)
    : IQuery<IReadOnlyList<MessageResponse>>;