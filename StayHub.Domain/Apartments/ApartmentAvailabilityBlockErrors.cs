using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public static class ApartmentAvailabilityBlockErrors
{
    public static Error NotFound = new(
        "ApartmentAvailabilityBlock.NotFound",
        "The availability block with the specified identifier was not found");

    public static Error Overlap = new(
        "ApartmentAvailabilityBlock.Overlap",
        "The block is overlapping with an existing booking or block");
}