using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Reviews;

namespace StayHub.Infrastructure.Repositories;

internal sealed class ReviewResponseRepository(ApplicationDbContext dbContext)
    : Repository<ReviewResponse>(dbContext), IReviewResponseRepository
{
    public async Task<ReviewResponse?> GetByReviewIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<ReviewResponse>()
            .FirstOrDefaultAsync(response => response.ReviewId == reviewId, cancellationToken);
    }
}