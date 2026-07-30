using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Auditing;

namespace StayHub.Infrastructure.Repositories;

internal sealed class AuditLogRepository(ApplicationDbContext dbContext)
    : Repository<AuditLog>(dbContext), IAuditLogRepository
{
    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<AuditLog>()
            .Where(log => log.EntityName == entityName && log.EntityId == entityId)
            .ToListAsync(cancellationToken);
    }
}