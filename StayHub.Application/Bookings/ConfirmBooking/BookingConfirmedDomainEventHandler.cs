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

public sealed class BookingConfirmedDomainEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<BookingConfirmedDomainEvent>
{
    public async Task Handle(
        BookingConfirmedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(domainEvent.BookingId, cancellationToken);
        if (booking is null) return;

        var now = dateTimeProvider.UtcNow;

        // Retrieve the conflicting reservations as tracked entities.
        var overlappingBookings =
            await bookingRepository.GetOverlappingReservedAsync(
                apartmentId: booking.ApartmentId,
                excludedBookingId: booking.Id,
                startDate: booking.Duration.Start,
                endDate: booking.Duration.End,
                cancellationToken);

        if (overlappingBookings.Count > 0)
        {
            foreach (var overlappingBooking in overlappingBookings)
            {
                // EMPTY GUID INDICATES TO AUTO REJECTION
                overlappingBooking.Reject(Guid.Empty, now);
            }
        }

        // A missing user must not prevent conflict resolution.
        var user = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        var payload = new BookingConfirmedNotificationPayload(
            BookingId: booking.Id,
            Message: "Your booking has been confirmed by the owner.");

            var notification = Notification.Create(
                user.Id,
                NotificationType.BookingConfirmed,
                JsonSerializer.Serialize(payload),
                now);

            notificationRepository.Add(notification);
        }

        // One unit-of-work save persists all tracked booking changes
        // and the notification, if one was created.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (user is not null)
        {
            await emailService.SendAsync(
                user.Email,
                "Booking confirmed!",
                "Your booking has been confirmed by the owner.");
        }
    }
}