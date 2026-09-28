using StayHub.Application.Abstractions.Caching;

namespace StayHub.Application.Apartments.GetMyApartmentsDashboard;

public sealed record GetMyApartmentsDashboardQuery(Guid OwnerId) : ICachedQuery<MyApartmentsDashboardResponse>
{
    public string CacheKey => CacheKeys.MyApartmentsDashboard(OwnerId);
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}