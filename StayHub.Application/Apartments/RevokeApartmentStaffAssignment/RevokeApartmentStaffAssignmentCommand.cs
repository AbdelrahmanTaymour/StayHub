using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.RevokeApartmentStaffAssignment;

public sealed record RevokeApartmentStaffAssignmentCommand(Guid AssignmentId, Guid RequestedByUserId) : ICommand;