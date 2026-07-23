using FluentValidation;

namespace StayHub.Application.Apartments.AssignApartmentStaff;

public class AssignApartmentStaffCommandValidator : AbstractValidator<AssignApartmentStaffCommand>
{
    public AssignApartmentStaffCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
        RuleFor(x => x.StaffUserId).NotEmpty();
        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Selected amenity is not supported.");
    }
}