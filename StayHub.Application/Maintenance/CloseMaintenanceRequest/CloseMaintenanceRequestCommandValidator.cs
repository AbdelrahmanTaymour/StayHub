using FluentValidation;

namespace StayHub.Application.Maintenance.CloseMaintenanceRequest;

internal sealed class CloseMaintenanceRequestCommandValidator : AbstractValidator<CloseMaintenanceRequestCommand>
{
    public CloseMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.MaintenanceRequestId).NotEmpty();
    }
}