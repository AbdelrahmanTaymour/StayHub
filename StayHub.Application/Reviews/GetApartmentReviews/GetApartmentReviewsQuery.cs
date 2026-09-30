using StayHub.Application.Abstractions.Caching;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Reviews.GetApartmentReviews;

public sealed record GetApartmentReviewsQuery(
    Guid ApartmentId,
    ReviewResponseStatusFilter ResponseStatus = ReviewResponseStatusFilter.All,
    ReviewRatingFilter Rating = ReviewRatingFilter.All,
    ReviewSortOrder SortOrder = ReviewSortOrder.Recent,
    int Page = 1,
    int PageSize = 10) : ICachedQuery<PagedResponse<ApartmentReviewResponse>>
{
    public string CacheKey => CacheKeys.ApartmentReviews(
        ApartmentId, ResponseStatus, Rating, SortOrder, Page, PageSize);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(2);
}

public enum ReviewResponseStatusFilter
{
    All = 0,
    NeedsResponse = 1,
    Responded = 2
}

public enum ReviewRatingFilter
{
    All = 0,
    FiveStars = 1,
    FourStars = 2,
    ThreeStarsOrLess = 3
}

public enum ReviewSortOrder
{
    Recent = 0,
    Oldest = 1,
    RatingDesc = 2,
    RatingAsc = 3
}