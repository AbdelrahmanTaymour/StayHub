using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.ConfirmBooking;

public class BookingConfirmedDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<BookingConfirmedDomainEvent>
{
    public async Task Handle(BookingConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var user = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (user is null) return;

        await emailService.SendAsync(
            user.Email,
            "Booking confirmed!",
            "Your booking has been confirmed by the owner.");
    }
}