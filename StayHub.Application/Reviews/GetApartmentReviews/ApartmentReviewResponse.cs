namespace StayHub.Application.Reviews.GetApartmentReviews;

public sealed record ApartmentReviewResponse
{
    public Guid Id { get; init; }
    public string ReviewerName { get; init; } = string.Empty;
    public string? ReviewerAvatarUrl { get; init; }
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
    public string? OwnerResponseComment { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}