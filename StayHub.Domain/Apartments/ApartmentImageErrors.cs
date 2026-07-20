using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public static class ApartmentImageErrors
{
    public static Error NotFound = new(
        "ApartmentImage.NotFound",
        "The image with the specified identifier was not found");

    public static Error InvalidOrderPayload = new(
        "ApartmentImage.InvalidOrderPayload",
        "The submitted order does not match the apartment's current set of images");
}