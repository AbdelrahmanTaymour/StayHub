using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Payments.RefundPayment;

public class PaymentRefundedDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<PaymentRefundedDomainEvent>
{
    public async Task Handle(PaymentRefundedDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var guest = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (guest is null) return;

        await emailService.SendAsync(
            guest.Email,
            "Payment refunded",
            "Your payment has been refunded.");
    }
}