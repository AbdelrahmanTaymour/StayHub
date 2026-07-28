using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public static class ApartmentAvailabilityBlockErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "ApartmentAvailabilityBlock.NotFound",
        "The availability block with the specified identifier was not found");

    public static readonly Error Overlap = Error.Conflict(
        "ApartmentAvailabilityBlock.Overlap",
        "The block is overlapping with an existing booking or block");
}