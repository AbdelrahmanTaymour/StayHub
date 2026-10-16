using StayHub.Domain.Apartments;

namespace StayHub.Api.Endpoints.Apartments;

public sealed record ChangeApartmentStaffRoleRequest(
    ApartmentStaffRole Role);