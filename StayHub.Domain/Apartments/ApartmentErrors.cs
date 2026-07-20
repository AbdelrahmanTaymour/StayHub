using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public static class ApartmentErrors
{
    public static Error NotFound = new(
        "Apartment.NotFound",
        "The apartment with the specified identifier was not found");

    public static Error AmenityAlreadyAdded = new(
        "Apartment.AmenityAlreadyAdded",
        "The amenity has already been added to this apartment");

    public static Error AmenityNotFound = new(
        "Apartment.AmenityNotFound",
        "The amenity was not found on this apartment");

    public static Error AlreadyActive = new(
        "Apartment.AlreadyActive",
        "The apartment is already active");

    public static Error AlreadyInactive = new(
        "Apartment.AlreadyInactive",
        "The apartment is already inactive");

    public static Error NotAuthorized = new(
        "Apartment.NotAuthorized",
        "Only the apartment owner can perform this action");
}