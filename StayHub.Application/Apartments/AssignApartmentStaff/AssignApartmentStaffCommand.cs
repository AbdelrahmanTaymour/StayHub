using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.AssignApartmentStaff;

public sealed record AssignApartmentStaffCommand(
    Guid ApartmentId,
    Guid StaffUserId,
    ApartmentStaffRole Role) : ICommand<Guid>;