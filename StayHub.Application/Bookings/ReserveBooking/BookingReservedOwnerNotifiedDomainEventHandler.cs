using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Bookings.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.ReserveBooking;

public class BookingReservedOwnerNotifiedDomainEventHandler(
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<BookingReservedDomainEvent>
{
    public async Task Handle(BookingReservedDomainEvent notification, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null) return;

        var apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null) return;

        var owner = await userRepository.GetByIdAsync(apartment.OwnerId, cancellationToken);

        if (owner is null) return;

        await emailService.SendAsync(
            owner.Email,
            "New booking request",
            "You have a new booking request awaiting your confirmation.");
    }
}