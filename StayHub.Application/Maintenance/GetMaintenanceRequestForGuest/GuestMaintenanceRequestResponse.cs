using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetMaintenanceRequestForGuest;

public sealed class GuestMaintenanceRequestResponse
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
    public DateTime? InProgressOnUtc { get; init; }
    public DateTime? ResolvedOnUtc { get; init; }
    public DateTime? ClosedOnUtc { get; init; }
}