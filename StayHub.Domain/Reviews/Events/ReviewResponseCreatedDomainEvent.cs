using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Reviews.Events;

public sealed record ReviewResponseCreatedDomainEvent(Guid ReviewResponseId, Guid ReviewId) : IDomainEvent;