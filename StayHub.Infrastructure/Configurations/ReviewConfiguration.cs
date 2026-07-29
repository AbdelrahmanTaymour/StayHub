using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Reviews;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(review => review.Id);

        builder.Property(review => review.Rating)
            .HasConversion(rating => rating.Value, value => Rating.Create(value).Value);

        builder.Property(review => review.Comment)
            .HasMaxLength(2000)
            .HasConversion(review => review.Value, value => new Comment(value));

        builder.HasIndex(review => review.BookingId).IsUnique();
        builder.HasIndex(review => review.ApartmentId);

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(review => review.ApartmentId);

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(review => review.BookingId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(review => review.UserId);
    }
}