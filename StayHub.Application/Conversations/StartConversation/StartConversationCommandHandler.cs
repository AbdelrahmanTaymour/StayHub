using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Realtime;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Application.Conversations.StartConversation;

internal sealed class StartConversationCommandHandler(
    IUserRepository userRepository,
    IApartmentRepository apartmentRepository,
    IBookingRepository bookingRepository,
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IRealtimeConversationNotifier notifier,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<StartConversationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        var initiatorId = userContext.UserId;

        if (initiatorId == request.RecipientId)
        {
            return Result.Failure<Guid>(ConversationErrors.CannotMessageSelf);
        }

        var recipientExists = await userRepository.ExistsAsync(request.RecipientId, cancellationToken);
        if (!recipientExists)
        {
            return Result.Failure<Guid>(ConversationErrors.RecipientNotFound);
        }

        Guid? apartmentId = null;
        if (request.ApartmentId is { } requestedApartmentId)
        {
            var apartment = await apartmentRepository.GetByIdAsync(requestedApartmentId, cancellationToken);

            if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

            apartmentId = apartment.Id;
        }

        Guid? bookingId = null;
        if (request.BookingId is { } requestedBookingId)
        {
            var booking = await bookingRepository.GetByIdAsync(requestedBookingId, cancellationToken);

            if (booking is null ||
                booking.UserId != initiatorId ||
                (apartmentId is not null && booking.ApartmentId != apartmentId))
            {
                return Result.Failure<Guid>(BookingErrors.NotFound);
            }

            bookingId = booking.Id;

            apartmentId ??= booking.ApartmentId;
        }

        var now = dateTimeProvider.UtcNow;

        var conversation = await conversationRepository.GetBetweenParticipantsAsync(
            initiatorId,
            request.RecipientId,
            cancellationToken);

        if (conversation is null)
        {
            var newConversation = Conversation.Start(
                initiatorId,
                request.RecipientId,
                apartmentId,
                bookingId,
                now);

            if (newConversation.IsFailure)
            {
                return Result.Failure<Guid>(newConversation.Error);
            }

            conversation = newConversation.Value;

            conversationRepository.Add(conversation);
        }
        else
        {
            // Attaching to a conversation that may already be open in
            // someone's thread, so (unlike the brand-new path) this one
            // also needs a ConversationUpdated push below, not just Inbox.
            if (apartmentId is not null) conversation.AttachApartment(apartmentId.Value);
            if (bookingId is not null) conversation.AttachBooking(bookingId.Value);
        }

        var message = Message.Send(
            conversation.Id,
            initiatorId,
            new MessageBody(request.InitialMessage),
            now);

        conversation.RegisterMessage(now);

        messageRepository.Add(message);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifier.NotifyConversationUpdatedAsync(conversation.Id, cancellationToken);

        await notifier.NotifyInboxUpdatedAsync(
            [conversation.InitiatorId, conversation.RecipientId],
            cancellationToken);

        return conversation.Id;
    }
}