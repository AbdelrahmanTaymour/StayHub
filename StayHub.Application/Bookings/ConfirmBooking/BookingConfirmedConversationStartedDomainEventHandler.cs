using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Realtime;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Conversations;

namespace StayHub.Application.Bookings.ConfirmBooking;

public class BookingConfirmedConversationStartedDomainEventHandler(
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IConversationRepository conversationRepository,
    IUnitOfWork unitOfWork,
    IRealtimeConversationNotifier notifier,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<BookingConfirmedDomainEvent>
{
    public async Task Handle(BookingConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null) return;

        var conversation = await conversationRepository.GetBetweenParticipantsAsync(
            booking.UserId,
            apartment.OwnerId,
            cancellationToken);

        bool wasExisting = conversation is not null;

        if (conversation is null)
        {
            conversation = Conversation.Start(
                booking.UserId,
                apartment.OwnerId,
                apartment.Id,
                booking.Id,
                dateTimeProvider.UtcNow).Value;

            conversationRepository.Add(conversation);
        }
        else
        {
            conversation.AttachApartment(apartment.Id);
            conversation.AttachBooking(booking.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Only meaningful for the reuse path — a brand-new conversation
        // can't be open in anyone's reservation-details pane yet.
        if (wasExisting)
        {
            await notifier.NotifyConversationUpdatedAsync(conversation.Id, cancellationToken);
        }
    }
}