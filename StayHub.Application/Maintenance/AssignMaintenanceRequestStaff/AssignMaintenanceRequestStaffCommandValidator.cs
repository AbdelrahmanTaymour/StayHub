using FluentValidation;

namespace StayHub.Application.Maintenance.AssignMaintenanceRequestStaff;

internal sealed class
    AssignMaintenanceRequestStaffCommandValidator : AbstractValidator<AssignMaintenanceRequestStaffCommand>
{
    public AssignMaintenanceRequestStaffCommandValidator()
    {
        RuleFor(x => x.MaintenanceRequestId).NotEmpty();
        RuleFor(x => x.StaffUserId).NotEmpty();
    }
}