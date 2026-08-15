using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.DeviceInfo)
            .HasMaxLength(500)
            .HasConversion(deviceInfo => deviceInfo.Value, value => new DeviceInfo(value));

        builder.Property(session => session.IpAddress)
            .HasMaxLength(45)
            .HasConversion(ip => ip.Value, value => IpAddress.Create(value).Value);

        builder.HasIndex(session => new { session.UserId, session.RevokedOnUtc });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(session => session.UserId);
    }
}