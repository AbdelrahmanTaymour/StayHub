namespace StayHub.Domain.Conversations;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Message>> GetByConversationIdAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        Guid conversationId,
        Guid recipientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Message>> GetUnreadForRecipientAsync(
        Guid conversationId,
        Guid recipientId,
        CancellationToken cancellationToken = default);

    void Add(Message message);
}