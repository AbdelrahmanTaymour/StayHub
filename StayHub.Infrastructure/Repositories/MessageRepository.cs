using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Conversations;

namespace StayHub.Infrastructure.Repositories;

internal sealed class MessageRepository(ApplicationDbContext dbContext)
    : Repository<Message>(dbContext), IMessageRepository
{
    public async Task<IReadOnlyList<Message>> GetByConversationIdAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Message>()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.SentOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid conversationId,
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Message>()
            .CountAsync(
                message =>
                    message.ConversationId == conversationId &&
                    message.SenderId != recipientId &&
                    message.ReadOnUtc == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetUnreadForRecipientAsync(
        Guid conversationId,
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Message>()
            .Where(message =>
                message.ConversationId == conversationId &&
                message.SenderId != recipientId &&
                message.ReadOnUtc == null)
            .ToListAsync(cancellationToken);
    }
}