namespace StayHub.Domain.Conversations;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Conversation?> GetBetweenParticipantsAsync(
        Guid apartmentId,
        Guid guestId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(Conversation conversation);
}