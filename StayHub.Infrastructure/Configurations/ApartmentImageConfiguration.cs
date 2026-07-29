using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Apartments;

namespace StayHub.Infrastructure.Configurations;

internal sealed class ApartmentImageConfiguration : IEntityTypeConfiguration<ApartmentImage>
{
    public void Configure(EntityTypeBuilder<ApartmentImage> builder)
    {
        builder.ToTable("apartment_images");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.Url)
            .HasMaxLength(2000)
            .HasConversion(image => image.Value, value => new ImageUrl(value));

        builder.HasIndex(image => new { image.ApartmentId, image.DisplayOrder });

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(image => image.ApartmentId);
    }
}