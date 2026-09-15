namespace StayHub.Application.Maintenance.CloseMaintenanceRequest;

public sealed record MaintenanceRequestClosedNotificationPayload(
    Guid MaintenanceRequestId,
    string Message);