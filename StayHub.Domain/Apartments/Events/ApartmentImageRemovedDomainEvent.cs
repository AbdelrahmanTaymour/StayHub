using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments.Events;

public record ApartmentImageRemovedDomainEvent(Guid Id, Guid ApartmentId, ImageUrl Url) : IDomainEvent;