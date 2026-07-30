using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Abstractions;

namespace StayHub.Infrastructure.Repositories;

internal abstract class Repository<T>
    where T : Entity
{
    protected ApplicationDbContext DbContext;

    protected Repository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<T>()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public void Add(T entity)
    {
        DbContext.Add(entity);
    }

    public void Remove(T entity)
    {
        DbContext.Remove(entity);
    }
}