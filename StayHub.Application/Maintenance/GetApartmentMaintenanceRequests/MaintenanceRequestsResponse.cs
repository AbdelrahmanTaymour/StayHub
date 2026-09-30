using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetApartmentMaintenanceRequests;

public sealed class MaintenanceRequestsResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public MaintenanceRequestStatus Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public Guid ReportedByUserId { get; init; }
    public string ReporterFirstName { get; init; } = string.Empty;
    public string ReporterLastName { get; init; } = string.Empty;
    public string? ReporterAvatarUrl { get; init; }
    public bool IsReportedByOwner { get; init; }
}