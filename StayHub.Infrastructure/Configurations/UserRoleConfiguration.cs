using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(userRole => new { userRole.UserId, userRole.RoleId });

        builder.HasOne<Role>().WithMany().HasForeignKey(userRole => userRole.RoleId);
    }
}