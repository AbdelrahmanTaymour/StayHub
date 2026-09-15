using StayHub.Application.Abstractions.Caching;

namespace StayHub.Application.Apartments.GetApartmentsByOwner;

public sealed record GetApartmentsByOwnerQuery(Guid OwnerId, bool IncludeInactive, int Page, int PageSize)
    : ICachedQuery<IReadOnlyList<ApartmentSummaryResponse>>
{
    // Inactive apartments are caller-specific data: authorization is evaluated
    // in the handler, while caching occurs before the handler executes.
    // Therefore, only the active-only variant is cacheable.
    public bool IsCacheable => !IncludeInactive;

    public string CacheKey => CacheKeys.ApartmentsByOwner(OwnerId, IncludeInactive, Page, PageSize);

    public TimeSpan? Expiration => TimeSpan.FromSeconds(1);
}