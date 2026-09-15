using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetMaintenanceRequest;

public sealed class MaintenanceRequestResponse
{
    public Guid Id { get; init; }

    public Guid ApartmentId { get; init; }

    public string ApartmentName { get; init; }

    public string ApartmentCity { get; init; }

    public string Title { get; init; }

    public string Description { get; init; }

    public MaintenanceRequestStatus Status { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? ResolvedOnUtc { get; init; }

    public DateTime? ClosedOnUtc { get; init; }

    public Guid ReportedByUserId { get; init; }

    public string ReporterFirstName { get; init; }

    public string ReporterLastName { get; init; }

    public string ReporterEmail { get; init; }

    public string? ReporterPhoneNumber { get; init; }

    public string? ReporterAvatarUrl { get; init; }
}