using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.CancelBooking;

internal sealed class CancelBookingCommandHandler(
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelBookingCommand>
{
    public async Task<Result> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
            return Result.Failure(BookingErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null)
            return Result.Failure(ApartmentErrors.NotFound);

        var isGuest = booking.UserId == userContext.UserId;
        var isOwner = apartment.OwnerId == userContext.UserId;
        var isAdmin = userContext.Roles.Contains(Role.Admin.Name);

        if (!isGuest && !isOwner && !isAdmin) return Result.Failure(BookingErrors.NotAuthorized);

        var result = booking.Cancel(dateTimeProvider.UtcNow);

        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}