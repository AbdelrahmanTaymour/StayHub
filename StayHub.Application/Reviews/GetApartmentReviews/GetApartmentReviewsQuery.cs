using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Reviews.GetApartmentReviews;

public sealed record GetApartmentReviewsQuery(
    Guid ApartmentId,
    int Page = 1,
    int PageSize = 10) : ICachedQuery<PagedResponse<ApartmentReviewResponse>>
{
    public string CacheKey => CacheKeys.ApartmentReviews(ApartmentId, Page, PageSize);
    public TimeSpan? Expiration => TimeSpan.FromMinutes(1);
}