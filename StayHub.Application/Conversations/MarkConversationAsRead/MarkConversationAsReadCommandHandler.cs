using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Realtime;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations;

namespace StayHub.Application.Conversations.MarkConversationAsRead;

internal sealed class MarkConversationAsReadCommandHandler(
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IRealtimeConversationNotifier notifier,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MarkConversationAsReadCommand>
{
    public async Task<Result> Handle(MarkConversationAsReadCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure(ConversationErrors.NotFound);

        if (conversation.GuestId != userContext.UserId && !userContext.IsOwner(conversation.OwnerId))
            return Result.Failure(ConversationErrors.NotAuthorized);

        var unreadMessages = await messageRepository.GetUnreadForRecipientAsync(
            conversation.Id,
            userContext.UserId,
            cancellationToken);

        foreach (var message in unreadMessages) message.MarkAsRead(dateTimeProvider.UtcNow);

        if (unreadMessages.Count > 0)
        {
            var now = dateTimeProvider.UtcNow;

            conversation.RegisterRead(userContext.UserId, now);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await notifier.NotifyConversationUpdatedAsync(conversation.Id, cancellationToken);
        await notifier.NotifyInboxUpdatedAsync([userContext.UserId], cancellationToken);


        return Result.Success();
    }
}