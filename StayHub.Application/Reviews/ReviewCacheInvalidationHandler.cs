namespace StayHub.Application.Reviews;

// TODO: REFACTOR CACHE INVALIDATION
/*public sealed class ReviewCacheInvalidationHandler(
    ICacheService cacheService,
    IReviewRepository reviewRepository)
    : INotificationHandler<ReviewCreatedDomainEvent>,
        INotificationHandler<ReviewResponseCreatedDomainEvent>
{
    public async Task Handle(ReviewCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        await cacheService.RemoveAsync(CacheKeys.ReviewsByApartment(
                notification.ApartmentId,
                1,
                20),
            cancellationToken);
    }

    public async Task Handle(ReviewResponseCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(notification.ReviewId, cancellationToken);

        if (review is null)
        {
            return;
        }

        await cacheService.RemoveAsync(CacheKeys.ReviewsByApartment(review.ApartmentId, 1, 20), cancellationToken);
    }
}*/