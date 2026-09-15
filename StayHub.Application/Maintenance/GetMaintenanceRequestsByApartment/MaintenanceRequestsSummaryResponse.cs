using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetMaintenanceRequestsByApartment;

public sealed class MaintenanceRequestsSummaryResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; }

    public MaintenanceRequestStatus Status { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public Guid ReportedByUserId { get; init; }

    public string ReporterFirstName { get; init; }

    public string ReporterLastName { get; init; }

    public string? ReporterAvatarUrl { get; init; }
}