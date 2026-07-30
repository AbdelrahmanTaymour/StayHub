using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Apartments;

namespace StayHub.Infrastructure.Repositories;

internal sealed class ApartmentStaffAssignmentRepository(ApplicationDbContext dbContext)
    : Repository<ApartmentStaffAssignment>(dbContext), IApartmentStaffAssignmentRepository
{
    public async Task<ApartmentStaffAssignment?> GetActiveAsync(
        Guid apartmentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<ApartmentStaffAssignment>()
            .FirstOrDefaultAsync(
                assignment =>
                    assignment.ApartmentId == apartmentId &&
                    assignment.UserId == userId &&
                    assignment.RevokedOnUtc == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ApartmentStaffAssignment>> GetActiveByApartmentIdAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<ApartmentStaffAssignment>()
            .Where(assignment => assignment.ApartmentId == apartmentId && assignment.RevokedOnUtc == null)
            .ToListAsync(cancellationToken);
    }
}