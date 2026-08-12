using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.GetMyConversations;

public sealed record GetMyConversationsQuery() : IQuery<IReadOnlyList<ConversationSummaryResponse>>;