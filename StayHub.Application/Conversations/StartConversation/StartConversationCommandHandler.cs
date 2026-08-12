using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Conversations;

namespace StayHub.Application.Conversations.StartConversation;

internal sealed class StartConversationCommandHandler(
    IApartmentRepository apartmentRepository,
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<StartConversationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        var guestId = userContext.UserId;

        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

        if (apartment.OwnerId == guestId)
        {
            return Result.Failure<Guid>(ConversationErrors.CannotMessageSelf);
        }

        var now = dateTimeProvider.UtcNow;

        var conversation = await conversationRepository.GetBetweenParticipantsAsync(
            apartment.Id,
            guestId,
            apartment.OwnerId,
            cancellationToken);

        if (conversation is null)
        {
            conversation = Conversation.Start(
                apartment.Id,
                null,
                guestId,
                apartment.OwnerId,
                now);

            conversationRepository.Add(conversation);
        }

        var message = Message.Send(
            conversation.Id,
            guestId,
            new MessageBody(request.InitialMessage),
            now);

        conversation.RegisterMessage(now);

        messageRepository.Add(message);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return conversation.Id;
    }
}