namespace StayHub.Application.Maintenance.ResolveMaintenanceRequest;

public sealed record MaintenanceRequestResolvedNotificationPayload(
    Guid MaintenanceRequestId,
    string Message);