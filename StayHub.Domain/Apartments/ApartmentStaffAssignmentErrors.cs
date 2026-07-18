using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public sealed class ApartmentStaffAssignmentErrors
{
    public static Error NotFound = new(
        "ApartmentStaffAssignment.NotFound",
        "The staff assignment with the specified identifier was not found");
 
    public static Error AlreadyAssigned = new(
        "ApartmentStaffAssignment.AlreadyAssigned",
        "This user is already assigned to this apartment");
 
    public static Error AlreadyRevoked = new(
        "ApartmentStaffAssignment.AlreadyRevoked",
        "This staff assignment has already been revoked");
}