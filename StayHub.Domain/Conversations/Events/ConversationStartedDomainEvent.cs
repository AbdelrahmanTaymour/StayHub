using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Conversations.Events;

public record ConversationStartedDomainEvent(Guid Id) : IDomainEvent;