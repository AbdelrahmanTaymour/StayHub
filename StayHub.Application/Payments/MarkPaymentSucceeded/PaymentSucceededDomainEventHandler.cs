using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Payments.MarkPaymentSucceeded;

public class PaymentSucceededDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<PaymentSucceededDomainEvent>
{
    public async Task Handle(PaymentSucceededDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var guest = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (guest is null) return;

        await emailService.SendAsync(
            guest.Email,
            "Payment received",
            "Your payment has been processed successfully.");
    }
}