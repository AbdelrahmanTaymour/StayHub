using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Repositories;

internal sealed class UserProfileRepository(ApplicationDbContext dbContext)
    : Repository<UserProfile>(dbContext), IUserProfileRepository
{
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<UserProfile>()
            .FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
    }
}