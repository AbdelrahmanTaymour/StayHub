using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.ChangeApartmentStaffRole;

internal sealed class ChangeApartmentStaffRoleCommandHandler(
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IApartmentRepository apartmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeApartmentStaffRoleCommand>
{
    public async Task<Result> Handle(ChangeApartmentStaffRoleCommand request, CancellationToken cancellationToken)
    {
        var assignment =
            await staffAssignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken);

        if (assignment is null)
            return Result.Failure(ApartmentStaffAssignmentErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(assignment.ApartmentId, cancellationToken);

        if (apartment is null)
            return Result.Failure(ApartmentErrors.NotFound);

        if (!userContext.IsOwner(apartment.OwnerId) && !userContext.IsAdmin)
            return Result.Failure(ApartmentErrors.NotAuthorized);

        var result = assignment.ChangeRole(request.Role);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}