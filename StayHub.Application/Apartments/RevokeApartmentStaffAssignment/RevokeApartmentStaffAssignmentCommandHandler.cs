using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.Apartments.RevokeApartmentStaffAssignment;

internal sealed class RevokeApartmentStaffAssignmentCommandHandler(
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IApartmentRepository apartmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RevokeApartmentStaffAssignmentCommand>
{
    public async Task<Result> Handle(
        RevokeApartmentStaffAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await staffAssignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken);

        if (assignment is null) return Result.Failure(ApartmentStaffAssignmentErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(assignment.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (apartment.OwnerId != userContext.UserId &&
            !userContext.Roles.Contains(Role.Admin.Name))
            return Result.Failure(ApartmentErrors.NotAuthorized);

        var result = assignment.Revoke(dateTimeProvider.UtcNow);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}