using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartmentStaff;

public sealed record ApartmentStaffResponse
{
    public Guid AssignmentId { get; init; }
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? PhoneNumber { get; init; }
    public ApartmentStaffRole Role { get; init; }
    public DateTime AssignedOnUtc { get; init; }
}