using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations;

namespace StayHub.Application.Conversations.SendMessage;

internal sealed class SendMessageCommandHandler(
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<SendMessageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var senderId = userContext.UserId;

        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure<Guid>(ConversationErrors.NotFound);

        if (conversation.GuestId != senderId &&
            conversation.OwnerId != senderId)
            return Result.Failure<Guid>(MessageErrors.NotAuthorized);

        var now = dateTimeProvider.UtcNow;

        var message = Message.Send(
            conversation.Id,
            senderId,
            new MessageBody(request.Body),
            now);

        conversation.RegisterMessage(now);

        messageRepository.Add(message);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}