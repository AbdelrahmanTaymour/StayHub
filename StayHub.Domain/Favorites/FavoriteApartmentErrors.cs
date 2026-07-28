using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Favorites;

public static class FavoriteApartmentErrors
{
    public static readonly Error AlreadyFavorited = Error.Conflict(
        "FavoriteApartment.AlreadyFavorited",
        "This apartment is already in the user's favorites");

    public static readonly Error NotFound = Error.NotFound(
        "FavoriteApartment.NotFound",
        "This apartment is not in the user's favorites");
}