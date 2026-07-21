namespace StayHub.Domain.Auditing;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default);

    void Add(AuditLog auditLog);
}