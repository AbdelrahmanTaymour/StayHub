using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Bookings.ConfirmBooking;

internal sealed class ConfirmBookingCommandHandler(
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ConfirmBookingCommand>
{
    public async Task<Result> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;

        var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
            return Result.Failure(BookingErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null)
            return Result.Failure(ApartmentErrors.NotFound);

        if (!userContext.IsOwner(apartment.OwnerId) && !userContext.IsAdmin)
            return Result.Failure(BookingErrors.NotAuthorized);

        var result = booking.Confirm(utcNow);

        if (result.IsFailure)
            return result;

        apartment.UpdateLastBooked(utcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}