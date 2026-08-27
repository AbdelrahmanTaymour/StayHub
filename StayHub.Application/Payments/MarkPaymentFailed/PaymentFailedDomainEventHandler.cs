using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Payments.MarkPaymentFailed;

public class PaymentFailedDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<PaymentFailedDomainEvent>
{
    public async Task Handle(PaymentFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var guest = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (guest is null) return;


        await emailService.SendAsync(
            guest.Email,
            "Payment failed",
            "We couldn't process your payment. Please try again.");
    }
}