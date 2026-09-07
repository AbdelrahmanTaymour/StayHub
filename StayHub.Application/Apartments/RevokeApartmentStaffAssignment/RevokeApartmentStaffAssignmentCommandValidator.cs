using FluentValidation;

namespace StayHub.Application.Apartments.RevokeApartmentStaffAssignment;

internal sealed class
    RevokeApartmentStaffAssignmentCommandValidator : AbstractValidator<RevokeApartmentStaffAssignmentCommand>
{
    public RevokeApartmentStaffAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId).NotEmpty();
    }
}