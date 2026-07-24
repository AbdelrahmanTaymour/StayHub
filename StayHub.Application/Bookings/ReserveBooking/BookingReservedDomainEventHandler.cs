using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.ReserveBooking;

public class BookingReservedDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<BookingReservedDomainEvent>
{
    public async Task Handle(BookingReservedDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var user = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (user is null) return;

        await emailService.SendAsync(
            user.Email,
            "Booking reserved!",
            "You have minutes to confirm your booking.");
    }
}