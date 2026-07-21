using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Conversations.Events;

public sealed record ConversationStartedDomainEvent(Guid ConversationId) : IDomainEvent;