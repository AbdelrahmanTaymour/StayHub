using StayHub.Domain.Maintenance;

namespace StayHub.Infrastructure.Repositories;

internal sealed class MaintenanceRequestRepository(ApplicationDbContext dbContext)
    : Repository<MaintenanceRequest>(dbContext), IMaintenanceRequestRepository
{
}