using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Apartments;

namespace StayHub.Infrastructure.Repositories;

internal sealed class ApartmentImageRepository(ApplicationDbContext dbContext)
    : Repository<ApartmentImage>(dbContext), IApartmentImageRepository
{
    public async Task<IReadOnlyList<ApartmentImage>> GetByApartmentIdAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<ApartmentImage>()
            .Where(image => image.ApartmentId == apartmentId)
            .OrderBy(image => image.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByApartmentId(Guid apartmentId, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<ApartmentImage>()
            .Where(image => image.ApartmentId == apartmentId)
            .CountAsync(cancellationToken);
    }
}