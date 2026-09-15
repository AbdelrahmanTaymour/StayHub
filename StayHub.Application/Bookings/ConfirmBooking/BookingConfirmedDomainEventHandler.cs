using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.ConfirmBooking;

public class BookingConfirmedDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<BookingConfirmedDomainEvent>
{
    public async Task Handle(BookingConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(domainEvent.BookingId, cancellationToken);

        if (booking is null) return;

        var user = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (user is null) return;

        var payload = new BookingConfirmedNotificationPayload(
            BookingId: booking.Id,
            ApartmentId: booking.ApartmentId,
            Message: "Your booking has been confirmed by the owner.");

        var notification = Notification.Create(
            user.Id,
            NotificationType.BookingConfirmed,
            JsonSerializer.Serialize(payload),
            dateTimeProvider.UtcNow);

        notificationRepository.Add(notification);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(
            user.Email,
            "Booking confirmed!",
            "Your booking has been confirmed by the owner.");
    }
}