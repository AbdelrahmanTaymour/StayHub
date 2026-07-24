using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.CancelBooking;

public class BookingCancelledDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<BookingCancelledDomainEvent>
{
    public async Task Handle(BookingCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var user = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (user is null) return;

        await emailService.SendAsync(
            user.Email,
            "Booking cancelled",
            "Your booking has been cancelled.");
    }
}