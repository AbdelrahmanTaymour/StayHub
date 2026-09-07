using FluentValidation;

namespace StayHub.Application.Maintenance.StartMaintenanceRequest;

internal sealed class StartMaintenanceRequestCommandValidator : AbstractValidator<StartMaintenanceRequestCommand>
{
    public StartMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.MaintenanceRequestId).NotEmpty();
    }
}