namespace StayHub.Domain.Reviews;

public interface IReviewResponseRepository
{
    Task<ReviewResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReviewResponse?> GetByReviewIdAsync(Guid reviewId, CancellationToken cancellationToken = default);

    void Add(ReviewResponse response);
}