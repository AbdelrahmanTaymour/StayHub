using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Favorites;

public static class FavoriteApartmentErrors
{
    public static Error AlreadyFavorited = new(
        "FavoriteApartment.AlreadyFavorited",
        "This apartment is already in the user's favorites");

    public static Error NotFound = new(
        "FavoriteApartment.NotFound",
        "This apartment is not in the user's favorites");
}