using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Repositories;

internal sealed class UserSessionRepository(ApplicationDbContext dbContext)
    : Repository<UserSession>(dbContext), IUserSessionRepository
{
    public async Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<UserSession>()
            .Where(session => session.UserId == userId && session.RevokedOnUtc == null)
            .ToListAsync(cancellationToken);
    }
}