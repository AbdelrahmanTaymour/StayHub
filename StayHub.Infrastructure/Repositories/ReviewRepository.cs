using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Reviews;

namespace StayHub.Infrastructure.Repositories;

internal sealed class ReviewRepository(ApplicationDbContext dbContext)
    : Repository<Review>(dbContext), IReviewRepository
{
    public async Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Review>()
            .AnyAsync(review => review.BookingId == bookingId, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetByApartmentIdAsync(
        Guid apartmentId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Review>()
            .Where(review => review.ApartmentId == apartmentId)
            .ToListAsync(cancellationToken);
    }
}