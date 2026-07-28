using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public static class ApartmentStaffAssignmentErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "ApartmentStaffAssignment.NotFound",
        "The staff assignment with the specified identifier was not found");

    public static readonly Error AlreadyAssigned = Error.Conflict(
        "ApartmentStaffAssignment.AlreadyAssigned",
        "This user is already assigned to this apartment");

    public static readonly Error AlreadyRevoked = Error.Conflict(
        "ApartmentStaffAssignment.AlreadyRevoked",
        "This staff assignment has already been revoked");
}