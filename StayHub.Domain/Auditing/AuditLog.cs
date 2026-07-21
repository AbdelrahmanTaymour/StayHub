using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Auditing;

public sealed class AuditLog : Entity
{
    private AuditLog(
        Guid id,
        Guid? userId,
        string entityName,
        string entityId,
        AuditAction action,
        string changes,
        DateTime occurredOnUtc)
        : base(id)
    {
        UserId = userId;
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        Changes = changes;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid? UserId { get; private set; }
    public string EntityName { get; private set; }
    public string EntityId { get; private set; }
    public AuditAction Action { get; private set; }
    public string Changes { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }

    public static AuditLog Create(
        Guid? userId,
        string entityName,
        string entityId,
        AuditAction action,
        string changes)
    {
        return new AuditLog(
            Guid.CreateVersion7(),
            userId, entityName,
            entityId,
            action,
            changes,
            DateTime.UtcNow);
    }
}