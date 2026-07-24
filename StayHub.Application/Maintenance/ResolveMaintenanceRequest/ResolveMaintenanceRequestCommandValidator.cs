using FluentValidation;

namespace StayHub.Application.Maintenance.ResolveMaintenanceRequest;

public class ResolveMaintenanceRequestCommandValidator : AbstractValidator<ResolveMaintenanceRequestCommand>
{
    public ResolveMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.MaintenanceRequestId).NotEmpty();
    }
}