using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name).HasMaxLength(50);

        builder.HasIndex(role => role.Name).IsUnique();

        // Not EF-mapped navigations - role assignment goes through UserRole/RolePermission as
        // plain FK-only join rows (see UserRoleConfiguration/RolePermissionConfiguration), never
        // through these object-reference collections, to avoid EF tracking a "new" insert for a
        // static reference-data object like Role.Guest that already exists via HasData.
        builder.Ignore(role => role.Users);
        builder.Ignore(role => role.Permissions);

        builder.HasData(Role.Guest, Role.Admin);
    }
}