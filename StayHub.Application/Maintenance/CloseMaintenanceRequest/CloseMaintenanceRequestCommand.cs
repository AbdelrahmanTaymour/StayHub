using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Maintenance.CloseMaintenanceRequest;

public sealed record CloseMaintenanceRequestCommand(Guid MaintenanceRequestId, Guid RequestedByUserId) : ICommand;