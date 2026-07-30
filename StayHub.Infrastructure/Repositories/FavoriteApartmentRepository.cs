using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Favorites;

namespace StayHub.Infrastructure.Repositories;

internal sealed class FavoriteApartmentRepository(ApplicationDbContext dbContext)
    : Repository<FavoriteApartment>(dbContext), IFavoriteApartmentRepository
{
    public async Task<FavoriteApartment?> GetAsync(
        Guid userId,
        Guid apartmentId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<FavoriteApartment>()
            .FirstOrDefaultAsync(
                favorite => favorite.UserId == userId && favorite.ApartmentId == apartmentId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<FavoriteApartment>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<FavoriteApartment>()
            .Where(favorite => favorite.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}