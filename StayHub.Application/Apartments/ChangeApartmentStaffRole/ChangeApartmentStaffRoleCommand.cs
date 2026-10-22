using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.ChangeApartmentStaffRole;

public sealed record ChangeApartmentStaffRoleCommand(
    Guid AssignmentId,
    ApartmentStaffRole Role) : ICommand;