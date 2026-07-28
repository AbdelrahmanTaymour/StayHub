using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public static class ApartmentErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Apartment.NotFound",
        "The apartment with the specified identifier was not found");

    public static readonly Error AmenityAlreadyAdded = Error.Conflict(
        "Apartment.AmenityAlreadyAdded",
        "The amenity has already been added to this apartment");

    public static readonly Error AmenityNotFound = Error.NotFound(
        "Apartment.AmenityNotFound",
        "The amenity was not found on this apartment");

    public static readonly Error AlreadyActive = Error.Conflict(
        "Apartment.AlreadyActive",
        "The apartment is already active");

    public static readonly Error AlreadyInactive = Error.Conflict(
        "Apartment.AlreadyInactive",
        "The apartment is already inactive");

    public static readonly Error NotAuthorized = Error.Unauthorized(
        "Apartment.NotAuthorized",
        "Only the apartment owner can perform this action");
}