namespace StayHub.Domain.Favorites;

public interface IFavoriteApartmentRepository
{
    Task<FavoriteApartment?> GetAsync(Guid userId, Guid apartmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteApartment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(FavoriteApartment favorite);

    void Remove(FavoriteApartment favorite);
}