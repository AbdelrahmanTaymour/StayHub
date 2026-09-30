using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.SearchStaffCandidate;

public sealed record StaffCandidateResponse
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? PhoneNumber { get; init; }
    public bool IsApartmentOwner { get; init; }
    public bool IsAlreadyAssigned { get; init; }
    public ApartmentStaffRole? CurrentRole { get; init; }
}