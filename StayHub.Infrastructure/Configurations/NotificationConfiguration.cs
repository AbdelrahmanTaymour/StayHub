using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Payload).HasColumnType("jsonb");

        builder.HasIndex(notification => new { notification.UserId, notification.IsRead, notification.CreatedOnUtc });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(notification => notification.UserId);
    }
}