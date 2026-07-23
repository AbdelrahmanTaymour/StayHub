using FluentValidation;

namespace StayHub.Application.Apartments.RevokeApartmentStaffAssignment;

public class RevokeApartmentStaffAssignmentCommandValidator : AbstractValidator<RevokeApartmentStaffAssignmentCommand>
{
    public RevokeApartmentStaffAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId).NotEmpty();
    }
}