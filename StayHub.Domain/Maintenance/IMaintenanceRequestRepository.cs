namespace StayHub.Domain.Maintenance;

public interface IMaintenanceRequestRepository
{
    Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);


    // TODO: COVERS THIS FUNCTIONALITY
    Task<IReadOnlyList<MaintenanceRequest>> GetByApartmentIdAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default);

    void Add(MaintenanceRequest request);
}