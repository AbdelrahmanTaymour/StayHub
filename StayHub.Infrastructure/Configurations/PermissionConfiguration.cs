using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Name).HasMaxLength(50);

        builder.HasIndex(permission => permission.Name).IsUnique();

        builder.HasData(
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
            Permission.MaintenanceManage);
    }
}