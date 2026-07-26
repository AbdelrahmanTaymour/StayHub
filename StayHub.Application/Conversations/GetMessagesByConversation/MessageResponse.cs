namespace StayHub.Application.Conversations.GetMessagesByConversation;

public sealed class MessageResponse
{
    public Guid Id { get; init; }

    public Guid SenderId { get; init; }

    public string Body { get; init; }

    public DateTime SentOnUtc { get; init; }

    public DateTime? ReadOnUtc { get; init; }
}