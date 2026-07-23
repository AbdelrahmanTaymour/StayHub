using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.RevokeApartmentStaffAssignment;

internal sealed class RevokeApartmentStaffAssignmentCommandHandler(
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IApartmentRepository apartmentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RevokeApartmentStaffAssignmentCommand>
{
    public async Task<Result> Handle(
        RevokeApartmentStaffAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await staffAssignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken);

        if (assignment is null) return Result.Failure(ApartmentStaffAssignmentErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(assignment.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure(ApartmentErrors.NotAuthorized);

        var result = assignment.Revoke();

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}