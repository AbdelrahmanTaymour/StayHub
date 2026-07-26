using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations;

namespace StayHub.Application.Conversations.SendMessage;

internal sealed class SendMessageCommandHandler(
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<SendMessageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure<Guid>(ConversationErrors.NotFound);

        if (conversation.GuestId != request.SenderId &&
            conversation.OwnerId != request.SenderId)
            return Result.Failure<Guid>(MessageErrors.NotAuthorized);

        var message = Message.Send(
            conversation.Id,
            request.SenderId,
            new MessageBody(request.Body),
            dateTimeProvider.UtcNow);

        messageRepository.Add(message);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}