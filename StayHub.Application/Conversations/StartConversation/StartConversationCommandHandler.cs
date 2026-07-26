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
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<StartConversationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

        var conversation = await conversationRepository.GetBetweenParticipantsAsync(
            apartment.Id,
            request.GuestId,
            apartment.OwnerId,
            cancellationToken);

        if (conversation is null)
        {
            conversation = Conversation.Start(apartment.Id, null, request.GuestId, apartment.OwnerId);
            conversationRepository.Add(conversation);
        }

        var message = Message.Send(
            conversation.Id,
            request.GuestId,
            new MessageBody(request.InitialMessage),
            dateTimeProvider.UtcNow);

        messageRepository.Add(message);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return conversation.Id;
    }
}