using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Application.Abstractions.Payments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Notifications;
using StayHub.Domain.Payments;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.CancelBooking;

public sealed class BookingCancelledDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IPaymentRepository paymentRepository,
    IPaymentGatewayService paymentGatewayService,
    INotificationRepository notificationRepository,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : INotificationHandler<BookingCancelledDomainEvent>
{
    public async Task Handle(
        BookingCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(domainEvent.BookingId, cancellationToken);

        if (booking is null) return;

        var user = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (user is null) return;

        var payment = await paymentRepository.GetByBookingIdAsync(booking.Id, cancellationToken);

        string message;

        if (payment is not null && payment.Status == PaymentStatus.Succeeded)
        {
            await paymentGatewayService.RefundAsync(payment.ProviderReference, cancellationToken);

            var result = payment.Refund(dateTimeProvider.UtcNow);

            if (result.IsFailure)
            {
                message =
                    "Your booking was cancelled, but we were unable to refund your payment. Please contact support.";

                var failedRefundPayload =
                    new BookingCancelledNotificationPayload(BookingId: booking.Id, Message: message);

                var failedRefundNotification = Notification.Create(
                    user.Id,
                    NotificationType.BookingCancelled,
                    JsonSerializer.Serialize(failedRefundPayload),
                    dateTimeProvider.UtcNow);

                notificationRepository.Add(failedRefundNotification);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                await emailService.SendAsync(user.Email, "Payment failed to refund", message);

                return;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            message = "Your booking has been cancelled. Your payment has been refunded.";
        }
        else
        {
            message = "Your booking has been cancelled.";
        }

        var payload = new BookingCancelledNotificationPayload(BookingId: booking.Id, Message: message);

        var notification = Notification.Create(
            user.Id,
            NotificationType.BookingCancelled,
            JsonSerializer.Serialize(payload),
            dateTimeProvider.UtcNow);

        notificationRepository.Add(notification);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(user.Email, "Booking cancelled", message);
    }
}