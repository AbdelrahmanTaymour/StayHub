using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Maintenance.GetMaintenanceRequest;

public sealed record GetMaintenanceRequestQuery(Guid MaintenanceRequestId) : IQuery<MaintenanceRequestResponse>;