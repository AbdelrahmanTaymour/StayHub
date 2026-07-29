using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(conversation => conversation.Id);

        builder.HasIndex(conversation => new { conversation.ApartmentId, conversation.GuestId, conversation.OwnerId })
            .IsUnique();

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(conversation => conversation.ApartmentId);

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(conversation => conversation.BookingId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(conversation => conversation.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(conversation => conversation.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}