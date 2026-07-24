using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.CompleteBooking;

public class BookingCompletedDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<BookingCompletedDomainEvent>
{
    public async Task Handle(BookingCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var user = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (user is null) return;

        await emailService.SendAsync(
            user.Email,
            "How was your stay?",
            "Your stay has ended - let others know how it went by leaving a review.");
    }
}