using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.CreateMaintenanceRequest;

internal sealed class CreateMaintenanceRequestCommandHandler(
    IApartmentRepository apartmentRepository,
    IBookingRepository bookingRepository,
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateMaintenanceRequestCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateMaintenanceRequestCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

        // If user is not the owner or admin, verify they are a tenant with an active booking
        if (!userContext.IsOwner(apartment.OwnerId) && !userContext.IsAdmin)
        {
            var hasActiveBooking = await bookingRepository.HasActiveBookingAsync(
                request.ApartmentId,
                userContext.UserId,
                dateTimeProvider.UtcNow,
                cancellationToken);

            if (!hasActiveBooking)
            {
                return Result.Failure<Guid>(MaintenanceRequestErrors.NotAuthorized);
            }
        }

        var maintenanceRequest = MaintenanceRequest.Create(
            request.ApartmentId,
            userContext.UserId,
            new Title(request.Title),
            new Description(request.Description),
            dateTimeProvider.UtcNow);

        maintenanceRequestRepository.Add(maintenanceRequest);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return maintenanceRequest.Id;
    }
}