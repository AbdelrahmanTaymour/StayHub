using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Application.Abstractions.Payments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Payments;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.CancelBooking;

public sealed class BookingCancelledDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IPaymentRepository paymentRepository,
    IPaymentGatewayService paymentGatewayService,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : INotificationHandler<BookingCancelledDomainEvent>
{
    public async Task Handle(
        BookingCancelledDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var user = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (user is null) return;

        var payment = await paymentRepository.GetByBookingIdAsync(booking.Id, cancellationToken);

        if (payment is not null && payment.Status == PaymentStatus.Succeeded)
        {
            await paymentGatewayService.RefundAsync(
                payment.ProviderReference,
                cancellationToken);

            var result = payment.Refund(dateTimeProvider.UtcNow);

            if (result.IsFailure)
            {
                await emailService.SendAsync(
                    user.Email,
                    "Payment failed to refund",
                    "Your booking was cancelled, but we were unable to refund your payment. Please contact support.");

                return;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await emailService.SendAsync(
            user.Email,
            "Booking cancelled",
            "Your booking has been cancelled.");
    }
}