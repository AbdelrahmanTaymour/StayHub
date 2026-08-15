using StayHub.Application.Abstractions.Caching;

namespace StayHub.Application.Reviews.GetReviewsByApartment;

public sealed record GetReviewsByApartmentQuery(Guid ApartmentId, int Page, int PageSize)
    : ICachedQuery<IReadOnlyList<ReviewListItemResponse>>
{
    public string CacheKey => CacheKeys.ReviewsByApartment(ApartmentId, Page, PageSize);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(3);
}