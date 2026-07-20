namespace StayHub.Domain.Apartments;

public interface IMaintenanceRequestRepository
{
    Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceRequest>> GetByApartmentIdAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default);

    void Add(MaintenanceRequest request);
}