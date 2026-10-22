using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Conversations;

namespace StayHub.Infrastructure.Repositories;

internal sealed class ConversationRepository(ApplicationDbContext dbContext)
    : Repository<Conversation>(dbContext), IConversationRepository
{
    public async Task<Conversation?> GetBetweenParticipantsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken = default)
    {
        // Order-independent: a conversation started by A->B must still be
        // found when later looked up as B->A (e.g. the owner later books
        // something that re-triggers this lookup from the booking side).
        return await DbContext
            .Set<Conversation>()
            .FirstOrDefaultAsync(
                conversation =>
                    (conversation.InitiatorId == userAId && conversation.RecipientId == userBId) ||
                    (conversation.InitiatorId == userBId && conversation.RecipientId == userAId),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Conversation>()
            .Where(conversation => conversation.InitiatorId == userId || conversation.RecipientId == userId)
            .ToListAsync(cancellationToken);
    }
}