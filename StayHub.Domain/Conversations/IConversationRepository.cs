namespace StayHub.Domain.Conversations;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Conversation?> GetBetweenParticipantsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default);

    void Add(Conversation conversation);
}