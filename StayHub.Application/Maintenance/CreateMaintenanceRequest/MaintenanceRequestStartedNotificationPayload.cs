namespace StayHub.Application.Maintenance.CreateMaintenanceRequest;

public sealed record MaintenanceRequestStartedNotificationPayload(
    Guid MaintenanceRequestId,
    string Message);