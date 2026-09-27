using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Conversations;

namespace StayHub.Application.Conversations.StartConversation;

internal sealed class StartConversationCommandHandler(
    IApartmentRepository apartmentRepository,
    IBookingRepository bookingRepository,
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

        Guid? bookingId = null;

        if (request.BookingId is { } requestedBookingId)
        {
            var booking = await bookingRepository.GetByIdAsync(requestedBookingId, cancellationToken);

            if (booking is null || booking.ApartmentId != apartment.Id || booking.UserId != guestId)
            {
                return Result.Failure<Guid>(BookingErrors.NotFound);
            }

            bookingId = booking.Id;
        }

        var now = dateTimeProvider.UtcNow;

        var conversation = await conversationRepository.GetBetweenParticipantsAsync(
            apartment.Id,
            guestId,
            apartment.OwnerId,
            cancellationToken);

        if (conversation is null)
        {
            var newConversation = Conversation.Start(
                apartment.Id,
                bookingId,
                guestId,
                apartment.OwnerId,
                now);

            if (newConversation.IsFailure)
            {
                return Result.Failure<Guid>(newConversation.Error);
            }

            conversation = newConversation.Value;

            conversationRepository.Add(conversation);
        }
        else if (bookingId is not null && conversation.BookingId is null)
        {
            // The conversation already existed (e.g. from an earlier inquiry) and now a booking
            // was made — attach it so the reservation-details pane picks it up going forward.
            conversation.AttachBooking(bookingId.Value);
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