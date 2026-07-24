using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Maintenance.ResolveMaintenanceRequest;

public sealed record ResolveMaintenanceRequestCommand(Guid MaintenanceRequestId, Guid RequestedByUserId) : ICommand;