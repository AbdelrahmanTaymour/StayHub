using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetApartmentMaintenanceRequests;

public sealed record GetApartmentMaintenanceRequestsQuery(
    Guid ApartmentId,
    string? Search = null,
    MaintenanceRequestStatus? Status = null,
    int Page = 1,
    int PageSize = 10)
    : ICachedQuery<PagedResponse<MaintenanceRequestsResponse>>
{
    public string CacheKey => CacheKeys.MaintenancesByApartment(ApartmentId, Search, Status, Page, PageSize);

    public TimeSpan? Expiration => TimeSpan.FromSeconds(1);
}