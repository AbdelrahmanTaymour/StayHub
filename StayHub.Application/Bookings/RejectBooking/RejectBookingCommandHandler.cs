using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.RejectBooking;

internal sealed class RejectBookingCommandHandler(
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RejectBookingCommand>
{
    public async Task<Result> Handle(RejectBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null) return Result.Failure(BookingErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        // Reject is strictly a Host/Admin action. Guests should use Cancel.
        var isOwner = apartment.OwnerId == userContext.UserId;
        var isAdmin = userContext.Roles.Contains(Role.Admin.Name);

        if (!isAdmin && !isOwner) return Result.Failure(BookingErrors.NotAuthorized);

        var result = booking.Reject(userContext.UserId, dateTimeProvider.UtcNow);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}