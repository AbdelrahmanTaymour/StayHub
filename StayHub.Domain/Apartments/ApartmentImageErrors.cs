using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public static class ApartmentImageErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "ApartmentImage.NotFound",
        "The image with the specified identifier was not found");

    public static readonly Error InvalidOrderPayload = Error.Validation(
        "ApartmentImage.InvalidOrderPayload",
        "The submitted order does not match the apartment's current set of images");
}