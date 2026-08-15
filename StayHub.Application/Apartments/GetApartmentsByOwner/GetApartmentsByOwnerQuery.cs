using StayHub.Application.Abstractions.Caching;

namespace StayHub.Application.Apartments.GetApartmentsByOwner;

public sealed record GetApartmentsByOwnerQuery(Guid OwnerId, int Page, int PageSize)
    : ICachedQuery<IReadOnlyList<ApartmentSummaryResponse>>
{
    public string CacheKey => CacheKeys.ApartmentsByOwner(OwnerId, Page, PageSize);

    public TimeSpan? Expiration => TimeSpan.FromSeconds(1);
}