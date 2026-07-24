using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.RejectBooking;

public class BookingRejectedDomainEventHandler(
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<BookingRejectedDomainEvent>
{
    public async Task Handle(BookingRejectedDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null) return;

        var guestRejected = notification.RejectedByUserId == booking.UserId;

        var recipientId = guestRejected ? apartment.OwnerId : booking.UserId;

        var recipient = await userRepository.GetByIdAsync(recipientId, cancellationToken);

        if (recipient is null) return;

        var message = guestRejected
            ? "The guest has withdrawn their booking request."
            : "The owner has declined your booking request.";

        await emailService.SendAsync(recipient.Email, "Booking request rejected", message);
    }
}