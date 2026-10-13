using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Conversations.Events;

public sealed record ConversationReadDomainEvent(
    Guid ConversationId,
    Guid ReaderId,
    DateTime ReadOnUtc) : IDomainEvent;