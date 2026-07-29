using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Reviews;

namespace StayHub.Infrastructure.Configurations;

internal sealed class ReviewResponseConfiguration : IEntityTypeConfiguration<ReviewResponse>
{
    public void Configure(EntityTypeBuilder<ReviewResponse> builder)
    {
        builder.ToTable("review_responses");

        builder.HasKey(response => response.Id);

        builder.Property(response => response.Comment)
            .HasMaxLength(2000)
            .HasConversion(response => response.Value, value => new Comment(value));

        builder.HasIndex(response => response.ReviewId).IsUnique();

        builder.HasOne<Review>()
            .WithMany()
            .HasForeignKey(response => response.ReviewId);
    }
}