using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Application.Maintenance.CloseMaintenanceRequest;

internal sealed class CloseMaintenanceRequestCommandHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IApartmentRepository apartmentRepository,
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CloseMaintenanceRequestCommand>
{
    public async Task<Result> Handle(CloseMaintenanceRequestCommand request, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            request.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null) return Result.Failure(MaintenanceRequestErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(maintenanceRequest.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        var isOwner = apartment.OwnerId == userContext.UserId;
        var isAdmin = userContext.Roles.Contains(Role.Admin.Name);
        var isActiveStaff = !isOwner && await staffAssignmentRepository.GetActiveAsync(
            apartment.Id,
            userContext.UserId,
            cancellationToken) is not null;

        if (!isOwner && !isAdmin && !isActiveStaff)
        {
            return Result.Failure(MaintenanceRequestErrors.NotAuthorized);
        }

        var result = maintenanceRequest.Close(dateTimeProvider.UtcNow);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}