using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Repositories;

internal sealed class UserRepository(ApplicationDbContext dbContext)
    : Repository<User>(dbContext), IUserRepository
{
    public async Task<User?> GetByIdentityId(string identityId, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<User>()
            .FirstOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(Domain.Users.Email email, CancellationToken cancellationToken = default)
    {
        return !await DbContext
            .Set<User>()
            .AnyAsync(user => user.Email == email, cancellationToken);
    }
}