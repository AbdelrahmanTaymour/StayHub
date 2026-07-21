using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Reviews.Events;

public sealed record ReviewCreatedDomainEvent(Guid ReviewId) : IDomainEvent;