using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Apartments;
using StayHub.Domain.Favorites;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class FavoriteApartmentConfiguration : IEntityTypeConfiguration<FavoriteApartment>
{
    public void Configure(EntityTypeBuilder<FavoriteApartment> builder)
    {
        builder.ToTable("favorite_apartments");

        builder.HasKey(favorite => favorite.Id);

        builder.HasIndex(favorite => new { favorite.UserId, favorite.ApartmentId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(favorite => favorite.UserId);

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(favorite => favorite.ApartmentId);
    }
}