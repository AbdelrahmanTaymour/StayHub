using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Configurations;

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Body)
            .HasMaxLength(4000)
            .HasConversion(message => message.Message, message => new MessageBody(message));

        builder.HasIndex(message => new { message.ConversationId, message.SentOnUtc });

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(message => message.ConversationId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}