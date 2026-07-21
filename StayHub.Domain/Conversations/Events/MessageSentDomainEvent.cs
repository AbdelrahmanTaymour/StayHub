using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Conversations.Events;

public record MessageSentDomainEvent(Guid Id, Guid ConversationId, Guid SenderId) : IDomainEvent;