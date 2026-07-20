using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users.Events;

public record UserSessionRevokedDomainEvent(Guid Id, Guid UserId) : IDomainEvent;