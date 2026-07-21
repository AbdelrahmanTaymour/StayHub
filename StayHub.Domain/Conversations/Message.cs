using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations.Events;

namespace StayHub.Domain.Conversations;

public sealed class Message : Entity
{
    private Message(
        Guid id,
        Guid conversationId,
        Guid senderId,
        MessageBody body,
        DateTime sentOnUtc)
        : base(id)
    {
        ConversationId = conversationId;
        SenderId = senderId;
        Body = body;
        SentOnUtc = sentOnUtc;
    }

    public Guid ConversationId { get; }
    public Guid SenderId { get; }
    public MessageBody Body { get; private set; }
    public DateTime SentOnUtc { get; private set; }
    public DateTime? ReadOnUtc { get; private set; }

    public static Message Sent(Guid conversationId, Guid senderId, MessageBody body, DateTime sentOnUtc)
    {
        var message = new Message(Guid.CreateVersion7(), conversationId, senderId, body, sentOnUtc);

        message.RaiseDomainEvent(new MessageSentDomainEvent(message.Id, message.ConversationId, message.SenderId));

        return message;
    }

    public Result MarkAdRead(DateTime utcNow)
    {
        if (ReadOnUtc is not null) return Result.Failure(MessageErrors.AlreadyRead);

        ReadOnUtc = utcNow;

        return Result.Success();
    }
}