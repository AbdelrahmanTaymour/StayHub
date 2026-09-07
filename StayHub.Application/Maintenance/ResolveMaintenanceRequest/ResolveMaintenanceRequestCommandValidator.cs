using FluentValidation;

namespace StayHub.Application.Maintenance.ResolveMaintenanceRequest;

internal sealed class ResolveMaintenanceRequestCommandValidator : AbstractValidator<ResolveMaintenanceRequestCommand>
{
    public ResolveMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.MaintenanceRequestId).NotEmpty();
    }
}