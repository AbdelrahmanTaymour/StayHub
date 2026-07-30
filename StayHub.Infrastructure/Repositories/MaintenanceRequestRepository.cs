using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Maintenance;

namespace StayHub.Infrastructure.Repositories;

internal sealed class MaintenanceRequestRepository(ApplicationDbContext dbContext)
    : Repository<MaintenanceRequest>(dbContext), IMaintenanceRequestRepository
{
    public async Task<IReadOnlyList<MaintenanceRequest>> GetByApartmentIdAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<MaintenanceRequest>()
            .Where(request => request.ApartmentId == apartmentId)
            .ToListAsync(cancellationToken);
    }
}