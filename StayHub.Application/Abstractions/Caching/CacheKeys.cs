namespace StayHub.Application.Abstractions.Caching;

/// <summary>
///     Central place for cache key naming - keeps the key a handler writes with and the key another
///     handler invalidates from staying in sync, instead of each file hand-rolling its own string.
/// </summary>
public static class CacheKeys
{
    public static string Apartment(Guid apartmentId)
    {
        return $"apartment:{apartmentId}";
    }

    public static string ApartmentSearch(string filtersAndPage)
    {
        return $"apartments:search:{filtersAndPage}";
    }

    public static string ApartmentsByOwner(Guid ownerId, int page, int pageSize)
    {
        return $"apartments:owner:{ownerId}:{page}:{pageSize}";
    }

    public static string ReviewsByApartment(Guid apartmentId, int page, int pageSize)
    {
        return $"reviews:apartment:{apartmentId}:{page}:{pageSize}";
    }

    public static string User(Guid userId)
    {
        return $"user:{userId}";
    }

    public static string LoggedInUser(Guid userId)
    {
        return $"user:me:{userId}";
    }
}