using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Apartments;

namespace StayHub.Infrastructure.Repositories;

internal sealed class ApartmentAvailabilityBlockRepository(ApplicationDbContext dbContext)
    : Repository<ApartmentAvailabilityBlock>(dbContext), IApartmentAvailabilityBlockRepository
{
    public async Task<IReadOnlyList<ApartmentAvailabilityBlock>> GetByApartmentIdAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<ApartmentAvailabilityBlock>()
            .Where(block => block.ApartmentId == apartmentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsOverlappingAsync(
        Guid apartmentId,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<ApartmentAvailabilityBlock>()
            .AnyAsync(
                block =>
                    block.ApartmentId == apartmentId &&
                    block.Start <= end &&
                    block.End >= start,
                cancellationToken);
    }
}