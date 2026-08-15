using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.AvatarUrl)
            .HasMaxLength(2000)
            .HasConversion(avatar => avatar.Url, value => new Avatar(value));

        builder.Property(profile => profile.Bio)
            .HasMaxLength(1000)
            .HasConversion(bio => bio.Value, value => new Bio(value));

        builder.Property(profile => profile.PhoneNumber)
            .HasMaxLength(30)
            .HasConversion(phoneNumber => phoneNumber.Value, value => PhoneNumber.Create(value).Value);

        builder.HasIndex(profile => profile.UserId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(profile => profile.UserId);
    }
}