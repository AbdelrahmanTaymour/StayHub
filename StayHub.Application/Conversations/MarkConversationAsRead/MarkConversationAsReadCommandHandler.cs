using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations;

namespace StayHub.Application.Conversations.MarkConversationAsRead;

internal sealed class MarkConversationAsReadCommandHandler(
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<MarkConversationAsReadCommand>
{
    public async Task<Result> Handle(MarkConversationAsReadCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure(ConversationErrors.NotFound);

        if (conversation.GuestId != request.RequestedByUserId &&
            conversation.OwnerId != request.RequestedByUserId)
            return Result.Failure(MessageErrors.NotAuthorized);

        var unreadMessages = await messageRepository.GetUnreadForRecipientAsync(
            conversation.Id,
            request.RequestedByUserId,
            cancellationToken);

        var utcNow = DateTime.UtcNow;

        foreach (var message in unreadMessages) message.MarkAsRead(utcNow);

        if (unreadMessages.Count > 0) await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}