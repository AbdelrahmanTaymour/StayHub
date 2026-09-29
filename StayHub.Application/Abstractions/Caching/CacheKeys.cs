using StayHub.Application.Apartments.GetApartmentsByOwner;
using StayHub.Application.Apartments.GetMyApartments;
using StayHub.Application.Reviews.GetApartmentReviews;
using StayHub.Domain.Maintenance;

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

    public static string ApartmentsByOwner(Guid ownerId, OwnerApartmentsSort sort, int page,
        int pageSize)
    {
        return $"apartments:owner:{ownerId}:{sort}:{page}:{pageSize}";
    }

    public static string MyApartments(
        Guid ownerId,
        MyApartmentsFilter status,
        string? search,
        int page,
        int pageSize)
    {
        return $"apartments:me:{ownerId}:{status}:{search}:{page}:{pageSize}";
    }

    public static string MyApartmentsVersion(Guid ownerId)
    {
        return $"apartments:me:version:{ownerId}";
    }

    public static string MyApartmentsDashboard(Guid ownerId)
    {
        return $"apartments:dashboard:{ownerId}";
    }

    public static string MaintenancesByApartment(Guid apartmentId, MaintenanceRequestStatus? status, int page,
        int pageSize)
    {
        return status != null
            ? $"maintenance:apartment:{apartmentId}:{status}:{page}:{pageSize}"
            : $"maintenance:apartment:{apartmentId}:{page}:{pageSize}";
    }

    public static string ApartmentReviews(
        Guid apartmentId,
        ReviewResponseStatusFilter responseStatus,
        ReviewRatingFilter rating,
        ReviewSortOrder sortOrder,
        int page,
        int pageSize)
    {
        return $"reviews:apartment:{apartmentId}:{responseStatus}:{rating}:{sortOrder}:{page}:{pageSize}";
    }

    public static string User(Guid userId)
    {
        return $"user:{userId}";
    }

    public static string OwnerProfile(Guid ownerId)
    {
        return $"user:owner:{ownerId}";
    }

    public static string LoggedInUser(Guid userId)
    {
        return $"user:me:{userId}";
    }
}