using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.AssignMaintenanceRequestStaff;

internal sealed class AssignMaintenanceRequestStaffCommandHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IApartmentRepository apartmentRepository,
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AssignMaintenanceRequestStaffCommand>
{
    public async Task<Result> Handle(AssignMaintenanceRequestStaffCommand request, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            request.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null) return Result.Failure(MaintenanceRequestErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(maintenanceRequest.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        var isOwner = userContext.IsOwner(apartment.OwnerId);
        var isAdmin = userContext.IsAdmin;

        if (!isOwner && !isAdmin)
            return Result.Failure(MaintenanceRequestErrors.NotAuthorized);

        var staffAssignment = await staffAssignmentRepository.GetActiveAsync(
            maintenanceRequest.ApartmentId,
            request.StaffUserId,
            cancellationToken);

        if (staffAssignment is null)
            return Result.Failure(MaintenanceRequestErrors.AssigneeIsNotActiveStaff);

        var result = maintenanceRequest.AssignStaff(request.StaffUserId, dateTimeProvider.UtcNow);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}