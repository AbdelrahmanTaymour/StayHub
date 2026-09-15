using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.RejectBooking;

public class BookingRejectedDomainEventHandler(
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<BookingRejectedDomainEvent>
{
    public async Task Handle(BookingRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(domainEvent.BookingId, cancellationToken);

        if (booking is null) return;

        var apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null) return;

        var guestRejected = domainEvent.RejectedByUserId == booking.UserId;

        var recipientId = guestRejected ? apartment.OwnerId : booking.UserId;

        var recipient = await userRepository.GetByIdAsync(recipientId, cancellationToken);

        if (recipient is null) return;

        var message = guestRejected
            ? "The guest has withdrawn their booking request."
            : "The owner has declined your booking request.";

        var payload = new BookingRejectedNotificationPayload(
            BookingId: booking.Id,
            ApartmentId: booking.ApartmentId,
            Message: message);

        var notification = Notification.Create(
            recipientId,
            NotificationType.BookingRejected,
            JsonSerializer.Serialize(payload),
            dateTimeProvider.UtcNow);

        notificationRepository.Add(notification);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(recipient.Email, "Booking request rejected", message);
    }
}