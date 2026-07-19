namespace StayHub.Domain.Apartments;

public interface IApartmentAvailabilityBlockRepository
{
    Task<ApartmentAvailabilityBlock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApartmentAvailabilityBlock>> GetByApartmentIdAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default);

    Task<bool> IsOverlappingAsync(
        Guid apartmentId,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default);

    void Add(ApartmentAvailabilityBlock block);

    void Remove(ApartmentAvailabilityBlock block);
}