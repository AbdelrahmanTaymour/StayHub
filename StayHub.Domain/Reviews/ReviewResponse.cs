using StayHub.Domain.Abstractions;
using StayHub.Domain.Reviews.Events;

namespace StayHub.Domain.Reviews;

public sealed class ReviewResponse : Entity
{
    private ReviewResponse(
        Guid id,
        Guid reviewId,
        Comment comment,
        DateTime createdOnUtc)
        : base(id)
    {
        ReviewId = reviewId;
        Comment = comment;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid ReviewId { get; }
    public Comment Comment { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public static ReviewResponse Create(Guid reviewId, Comment comment)
    {
        var response = new ReviewResponse(Guid.CreateVersion7(), reviewId, comment, DateTime.UtcNow);

        response.RaiseDomainEvent(new ReviewResponseCreatedDomainEvent(response.Id, response.ReviewId));

        return response;
    }

    public void UpdateComment(Comment comment)
    {
        Comment = comment;
    }
}