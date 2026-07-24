using FluentValidation;

namespace StayHub.Application.Maintenance.StartMaintenanceRequest;

public class StartMaintenanceRequestCommandValidator : AbstractValidator<StartMaintenanceRequestCommand>
{
    public StartMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.MaintenanceRequestId).NotEmpty();
    }
}