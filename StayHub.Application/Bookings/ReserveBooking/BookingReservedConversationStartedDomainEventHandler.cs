using MediatR;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Conversations;

namespace StayHub.Application.Bookings.ReserveBooking;

public class BookingReservedConversationStartedDomainEventHandler(
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IConversationRepository conversationRepository,
    IUnitOfWork unitOfWork) : INotificationHandler<BookingReservedDomainEvent>
{
    public async Task Handle(BookingReservedDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null) return;

        var existingConversation = await conversationRepository.GetBetweenParticipantsAsync(
            apartment.Id,
            booking.UserId,
            apartment.OwnerId,
            cancellationToken);

        if (existingConversation is not null) return;

        var conversation = Conversation.Start(apartment.Id, booking.Id, booking.UserId, apartment.OwnerId);

        conversationRepository.Add(conversation);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}