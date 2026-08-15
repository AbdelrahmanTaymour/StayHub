using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Apartments.GetApartmentsByOwner;

namespace StayHub.Application.Apartments.SearchApartments;

public sealed record SearchApartmentsQuery(
    string? City,
    decimal? MinPrice,
    decimal? MaxPrice,
    DateOnly? Start,
    DateOnly? End,
    int Page,
    int PageSize) : ICachedQuery<IReadOnlyList<ApartmentSummaryResponse>>
{
    public string CacheKey
    {
        get
        {
            var filtersAndPage = string.Join(
                '|',
                City,
                MinPrice,
                MaxPrice,
                Start,
                End,
                Page,
                PageSize);

            return CacheKeys.ApartmentSearch(filtersAndPage);
        }
    }

    public TimeSpan? Expiration => TimeSpan.FromMinutes(1);
}