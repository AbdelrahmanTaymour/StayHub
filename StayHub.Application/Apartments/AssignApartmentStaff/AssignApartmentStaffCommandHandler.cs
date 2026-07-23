using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.Apartments.AssignApartmentStaff;

public class AssignApartmentStaffCommandHandler(
    IApartmentRepository apartmentRepository,
    IUserRepository userRepository,
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AssignApartmentStaffCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AssignApartmentStaffCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure<Guid>(ApartmentErrors.NotAuthorized);

        var staffUser = await userRepository.GetByIdAsync(request.StaffUserId, cancellationToken);

        if (staffUser is null) return Result.Failure<Guid>(UserErrors.NotFound);

        var existingAssignment = await staffAssignmentRepository.GetActiveAsync(
            request.ApartmentId,
            request.StaffUserId,
            cancellationToken);

        if (existingAssignment is not null) return Result.Failure<Guid>(ApartmentStaffAssignmentErrors.AlreadyAssigned);

        var assignment = ApartmentStaffAssignment.Create(
            request.ApartmentId,
            request.StaffUserId,
            request.Role,
            dateTimeProvider.UtcNow);

        staffAssignmentRepository.Add(assignment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return assignment.Id;
    }
}