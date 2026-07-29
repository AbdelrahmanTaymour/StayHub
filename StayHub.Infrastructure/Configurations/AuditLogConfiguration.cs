using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Auditing;

namespace StayHub.Infrastructure.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.EntityName).HasMaxLength(200);
        builder.Property(log => log.EntityId).HasMaxLength(200);
        builder.Property(log => log.Changes).HasColumnType("jsonb");

        builder.HasIndex(log => new { log.EntityName, log.EntityId });
    }
}