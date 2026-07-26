using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.GetConversationsByUser;

public sealed record GetConversationsByUserQuery(Guid UserId) : IQuery<IReadOnlyList<ConversationSummaryResponse>>;