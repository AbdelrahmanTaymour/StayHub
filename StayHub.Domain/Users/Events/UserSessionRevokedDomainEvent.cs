using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users.Events;

public sealed record UserSessionRevokedDomainEvent(Guid UserSessionId, Guid UserId) : IDomainEvent;