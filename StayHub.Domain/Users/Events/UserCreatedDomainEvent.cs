using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users.Events;

public sealed record UserCreatedDomainEvent(Guid UserId) : IDomainEvent;