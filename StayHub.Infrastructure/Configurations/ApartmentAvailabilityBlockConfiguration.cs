using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Apartments;

namespace StayHub.Infrastructure.Configurations;

internal sealed class ApartmentAvailabilityBlockConfiguration : IEntityTypeConfiguration<ApartmentAvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<ApartmentAvailabilityBlock> builder)
    {
        builder.ToTable("apartment_availability_blocks");

        builder.HasKey(block => block.Id);

        builder.HasIndex(block => new { block.ApartmentId, block.Start, block.End });

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(block => block.ApartmentId);
    }
}