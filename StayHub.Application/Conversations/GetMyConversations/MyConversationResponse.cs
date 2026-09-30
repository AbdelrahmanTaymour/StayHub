namespace StayHub.Application.Conversations.GetMyConversations;

public sealed class MyConversationResponse
{
    public Guid Id { get; init; }

    public Guid ApartmentId { get; init; }

    public string ApartmentName { get; init; } = string.Empty;

    public Guid OtherPartyId { get; init; }

    public string OtherPartyName { get; init; } = string.Empty;

    public string? OtherPartyAvatarUrl { get; init; }

    public ConversationRole OtherPartyRole { get; init; }

    public string? LastMessagePreview { get; init; }

    public DateTime? LastMessageOnUtc { get; init; }

    public int UnreadCount { get; init; }
}