using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetMaintenanceRequestsByApartment;

public sealed record GetMaintenanceRequestsByApartmentQuery(
    Guid ApartmentId,
    MaintenanceRequestStatus? Status,
    int Page,
    int PageSize)
    : ICachedQuery<IReadOnlyList<MaintenanceRequestsSummaryResponse>>
{
    public string CacheKey => CacheKeys.MaintenancesByApartment(ApartmentId, Status, Page, PageSize);

    public TimeSpan? Expiration => TimeSpan.FromSeconds(1);
}