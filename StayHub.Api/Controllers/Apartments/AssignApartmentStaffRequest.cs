using StayHub.Domain.Apartments;

namespace StayHub.Api.Controllers.Apartments;

public sealed record AssignApartmentStaffRequest(Guid StaffUserId, ApartmentStaffRole Role);