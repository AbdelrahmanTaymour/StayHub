using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Apartments.GetApartmentsByOwner;

public sealed record GetApartmentsByOwnerQuery(
    Guid OwnerId,
    OwnerApartmentsSort Sort = OwnerApartmentsSort.PriceAsc,
    int Page = 1,
    int PageSize = 9)
    : ICachedQuery<PagedResponse<OwnerApartmentsResponse>>
{
    public string CacheKey => CacheKeys.ApartmentsByOwner(OwnerId, Sort, Page, PageSize);

    public TimeSpan? Expiration => TimeSpan.FromSeconds(1);
}