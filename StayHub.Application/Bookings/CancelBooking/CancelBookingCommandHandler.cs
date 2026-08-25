using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.CancelBooking;

internal sealed class CancelBookingCommandHandler(
    IBookingRepository bookingRepository,
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
        var isAdmin = userContext.Roles.Contains(Role.Admin.Name);

        if (!isGuest && !isAdmin)
            return Result.Failure(BookingErrors.NotAuthorized);

        var result = booking.Cancel(dateTimeProvider.UtcNow);

        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}