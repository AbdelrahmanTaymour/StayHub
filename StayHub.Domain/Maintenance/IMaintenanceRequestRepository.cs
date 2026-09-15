namespace StayHub.Domain.Maintenance;

public interface IMaintenanceRequestRepository
{
    Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(MaintenanceRequest request);
}