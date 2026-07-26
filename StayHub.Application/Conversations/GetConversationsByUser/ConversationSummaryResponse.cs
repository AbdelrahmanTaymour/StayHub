namespace StayHub.Application.Conversations.GetConversationsByUser;

public sealed class ConversationSummaryResponse
{
    public Guid Id { get; init; }

    public Guid ApartmentId { get; init; }

    public Guid GuestId { get; init; }

    public Guid OwnerId { get; init; }

    public DateTime? LastMessageOnUtc { get; init; }

    public int UnreadCount { get; init; }
}