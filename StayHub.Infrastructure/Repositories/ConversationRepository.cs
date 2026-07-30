using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Conversations;

namespace StayHub.Infrastructure.Repositories;

internal sealed class ConversationRepository(ApplicationDbContext dbContext)
    : Repository<Conversation>(dbContext), IConversationRepository
{
    public async Task<Conversation?> GetBetweenParticipantsAsync(
        Guid apartmentId,
        Guid guestId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Conversation>()
            .FirstOrDefaultAsync(
                conversation =>
                    conversation.ApartmentId == apartmentId &&
                    conversation.GuestId == guestId &&
                    conversation.OwnerId == ownerId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Conversation>()
            .Where(conversation => conversation.GuestId == userId || conversation.OwnerId == userId)
            .ToListAsync(cancellationToken);
    }
}