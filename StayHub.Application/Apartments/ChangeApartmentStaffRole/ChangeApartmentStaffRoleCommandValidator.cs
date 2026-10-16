using FluentValidation;

namespace StayHub.Application.Apartments.ChangeApartmentStaffRole;

internal sealed class ChangeApartmentStaffRoleCommandValidator : AbstractValidator<ChangeApartmentStaffRoleCommand>
{
    public ChangeApartmentStaffRoleCommandValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum();
    }
}