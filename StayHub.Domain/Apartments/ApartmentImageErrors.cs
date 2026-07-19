using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public static class ApartmentImageErrors
{
    public static Error NotFound = new(
        "ApartmentImage.NotFound",
        "The image with the specified identifier was not found");
}