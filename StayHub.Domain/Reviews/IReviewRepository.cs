namespace StayHub.Domain.Reviews;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Review>> GetByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default);

    void Add(Review review);
}