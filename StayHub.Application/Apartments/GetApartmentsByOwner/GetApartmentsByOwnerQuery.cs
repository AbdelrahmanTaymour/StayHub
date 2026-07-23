using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.GetApartmentsByOwner;

public sealed record GetApartmentsByOwnerQuery(Guid OwnerId, int Page, int PageSize)
    : IQuery<IReadOnlyList<ApartmentSummaryResponse>>;