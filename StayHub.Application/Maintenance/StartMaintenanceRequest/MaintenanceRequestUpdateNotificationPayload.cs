namespace StayHub.Application.Maintenance.StartMaintenanceRequest;

public sealed record MaintenanceRequestUpdateNotificationPayload(
    Guid MaintenanceRequestId,
    string Message);