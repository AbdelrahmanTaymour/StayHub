using FluentValidation;

namespace StayHub.Application.Maintenance.CloseMaintenanceRequest;

public class CloseMaintenanceRequestCommandValidator : AbstractValidator<CloseMaintenanceRequestCommand>
{
    public CloseMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.MaintenanceRequestId).NotEmpty();
    }
}