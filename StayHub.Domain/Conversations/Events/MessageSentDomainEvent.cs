using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Conversations.Events;

public sealed record MessageSentDomainEvent(Guid MessageId, Guid ConversationId, Guid SenderId) : IDomainEvent;