namespace StayHub.Application.Maintenance.CreateMaintenanceRequest;

public sealed record MaintenanceRequestCreatedNotificationPayload(
    Guid MaintenanceRequestId,
    Guid ApartmentId,
    string Comment);