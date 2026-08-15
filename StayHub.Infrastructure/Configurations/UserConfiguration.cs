using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FirstName)
            .HasMaxLength(100)
            .HasConversion(firstName => firstName.Value, value => new FirstName(value));

        builder.Property(user => user.LastName)
            .HasMaxLength(100)
            .HasConversion(lastName => lastName.Value, value => new LastName(value));

        builder.Property(user => user.Email)
            .HasMaxLength(300)
            .HasConversion(email => email.Value, value => Domain.Users.Email.Create(value).Value);

        builder.Property(user => user.IdentityId)
            .HasMaxLength(64);

        builder.HasIndex(user => user.Email).IsUnique();

        builder.HasIndex(user => user.IdentityId).IsUnique();

        builder.HasMany(u => u.Roles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId);

        builder.Metadata
            .FindNavigation(nameof(User.Roles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}