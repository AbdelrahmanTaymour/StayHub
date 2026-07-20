using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users.Events;

public record UserSessionCreatedDomainEvent(Guid Id, Guid UserId) : IDomainEvent;