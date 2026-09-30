using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetMaintenanceRequest;

public sealed class MaintenanceRequestResponse
{
    public Guid Id { get; init; }
    public Guid ApartmentId { get; init; }
    public string ApartmentName { get; init; } = string.Empty;
    public string ApartmentCity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public MaintenanceRequestStatus Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? StartOnUtc { get; init; }
    public DateTime? ResolvedOnUtc { get; init; }
    public DateTime? ClosedOnUtc { get; init; }
    public Guid ReportedByUserId { get; init; }
    public string ReporterFirstName { get; init; } = string.Empty;
    public string ReporterLastName { get; init; } = string.Empty;
    public string ReporterEmail { get; init; } = string.Empty;
    public string? ReporterPhoneNumber { get; init; }
    public string? ReporterAvatarUrl { get; init; }
    public Guid? AssignedToUserId { get; init; }
}