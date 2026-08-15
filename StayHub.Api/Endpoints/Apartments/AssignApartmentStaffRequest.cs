using StayHub.Domain.Apartments;

namespace StayHub.Api.Endpoints.Apartments;

public sealed record AssignApartmentStaffRequest(Guid StaffUserId, ApartmentStaffRole Role);