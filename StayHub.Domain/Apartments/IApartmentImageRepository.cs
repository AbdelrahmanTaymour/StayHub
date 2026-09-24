namespace StayHub.Domain.Apartments;

public interface IApartmentImageRepository
{
    Task<ApartmentImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApartmentImage>> GetByApartmentIdAsync(Guid apartmentId,
        CancellationToken cancellationToken = default);

    Task<ApartmentImage?> GetPrimaryByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default);

    void Add(ApartmentImage image);

    void Remove(ApartmentImage image);

    Task<int> CountByApartmentId(Guid apartmentId, CancellationToken cancellationToken = default);
}