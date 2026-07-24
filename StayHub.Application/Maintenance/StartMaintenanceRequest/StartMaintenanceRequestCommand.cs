using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Maintenance.StartMaintenanceRequest;

public sealed record StartMaintenanceRequestCommand(Guid MaintenanceRequestId, Guid RequestedByUserId) : ICommand;