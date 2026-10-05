using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Apartments.SearchApartments;

public sealed record CachedSearchApartmentsQuery(
    string? City = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    DateOnly? Start = null,
    DateOnly? End = null,
    int Page = 1,
    int PageSize = 20) : ICachedQuery<PagedResponse<ApartmentSearchResult>>
{
    public string CacheKey
    {
        get
        {
            var filtersAndPage = string.Join('|', City, MinPrice, MaxPrice, Start, End, Page, PageSize);
            return CacheKeys.ApartmentSearch(filtersAndPage);
        }
    }

    public TimeSpan? Expiration => TimeSpan.FromMinutes(1);
}