using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Bookings.CancelBooking;

internal sealed class CancelBookingCommandHandler(
    IBookingRepository bookingRepository,
    IApartmentAvailabilityBlockRepository availabilityBlockRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelBookingCommand>
{
    public async Task<Result> Handle(
        CancelBookingCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(
            request.BookingId,
            cancellationToken);

        if (booking is null)
            return Result.Failure(BookingErrors.NotFound);

        var isGuest = booking.UserId == userContext.UserId;
        var isAdmin = userContext.IsAdmin;

        if (!isGuest && !isAdmin)
            return Result.Failure(BookingErrors.NotAuthorized);

        var result = booking.Cancel(dateTimeProvider.UtcNow);

        if (result.IsFailure)
            return result;

        var availabilityBlock = await availabilityBlockRepository.GetByApartmentIdAndDateDurationAsync(
            booking.ApartmentId,
            booking.Duration.Start, booking.Duration.End, cancellationToken);

        if (availabilityBlock is not null)
            availabilityBlockRepository.Remove(availabilityBlock);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}