using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Apartments.GetMyApartments;

public enum MyApartmentsFilter
{
    All,
    Active,
    Inactive
}

public sealed record GetMyApartmentsQuery(
    Guid OwnerId,
    MyApartmentsFilter Status = MyApartmentsFilter.All,
    string? Search = null,
    int Page = 1,
    int PageSize = 10)
    : IVersionedCachedQuery<PagedResponse<MyApartmentsResponse>>
{
    public string CacheKey =>
        CacheKeys.MyApartments(
            OwnerId,
            Status,
            Search,
            Page,
            PageSize);

    public string VersionKey => CacheKeys.MyApartmentsVersion(OwnerId);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}