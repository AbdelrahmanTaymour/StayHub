using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users.Events;

public sealed record UserProfileUpdatedDomainEvent(Guid UserId) : IDomainEvent;