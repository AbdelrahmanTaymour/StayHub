using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users.Events;

public sealed record UserSessionCreatedDomainEvent(Guid Id, Guid UserId) : IDomainEvent;