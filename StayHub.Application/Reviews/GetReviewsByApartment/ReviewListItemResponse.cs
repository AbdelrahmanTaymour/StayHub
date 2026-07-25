namespace StayHub.Application.Reviews.GetReviewsByApartment;

public sealed class ReviewListItemResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public int Rating { get; init; }

    public string Comment { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public string? OwnerResponseComment { get; init; }
}