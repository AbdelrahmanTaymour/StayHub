using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    private static readonly Permission[] AllPermissions =
    [
        Permission.UserRead,
        Permission.UserUpdate,
        Permission.UserManageSessions,
        Permission.ApartmentCreate,
        Permission.ApartmentManage,
        Permission.BookingCreate,
        Permission.BookingManage,
        Permission.PaymentCreate,
        Permission.PaymentRefund,
        Permission.ReviewCreate,
        Permission.ReviewRespond,
        Permission.FavoriteManage,
        Permission.ConversationManage,
        Permission.NotificationManage,
        Permission.MaintenanceCreate,
        Permission.MaintenanceManage
    ];

    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });

        builder.HasOne<Role>().WithMany().HasForeignKey(rolePermission => rolePermission.RoleId);
        builder.HasOne<Permission>().WithMany().HasForeignKey(rolePermission => rolePermission.PermissionId);

        // Guest gets every current permission - there is no second tier yet that should be more
        // restricted (see docs/roles-and-permissions-design.md). Admin gets the same set for now,
        // so nothing breaks if a user is Admin instead of Guest, ready to gain exclusive
        // permissions later without a schema change.
        builder.HasData(
            AllPermissions.Select(permission => new RolePermission
            {
                RoleId = Role.Guest.Id,
                PermissionId = permission.Id
            }));

        builder.HasData(
            AllPermissions.Select(permission => new RolePermission
            {
                RoleId = Role.Admin.Id,
                PermissionId = permission.Id
            }));
    }
}