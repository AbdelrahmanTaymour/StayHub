using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations;

namespace StayHub.Application.Conversations.MarkConversationAsRead;

internal sealed class MarkConversationAsReadCommandHandler(
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MarkConversationAsReadCommand>
{
    public async Task<Result> Handle(MarkConversationAsReadCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure(ConversationErrors.NotFound);

        if (conversation.GuestId != userContext.UserId &&
            conversation.OwnerId != userContext.UserId)
        {
            return Result.Failure(ConversationErrors.NotAuthorized);
        }

        var unreadMessages = await messageRepository.GetUnreadForRecipientAsync(
            conversation.Id,
            userContext.UserId,
            cancellationToken);

        foreach (var message in unreadMessages) message.MarkAsRead(dateTimeProvider.UtcNow);

        if (unreadMessages.Count > 0) await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}