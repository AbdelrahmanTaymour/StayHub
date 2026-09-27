using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Apartments.GetApartmentsByOwner;

public sealed record GetApartmentsByOwnerQuery(
    Guid OwnerId,
    bool IncludeInactive,
    OwnerApartmentsSort Sort = OwnerApartmentsSort.PriceAsc,
    int Page = 1,
    int PageSize = 9)
    : ICachedQuery<PagedResponse<OwnerApartmentsResponse>>
{
    // Inactive apartments are caller-specific data: authorization is evaluated
    // in the handler, while caching occurs before the handler executes.
    // Therefore, only the active-only variant is cacheable.
    public bool IsCacheable => !IncludeInactive;

    public string CacheKey => CacheKeys.ApartmentsByOwner(OwnerId, IncludeInactive, Sort, Page, PageSize);

    public TimeSpan? Expiration => TimeSpan.FromSeconds(1);
}