using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Reviews;
using StayHub.Domain.Reviews.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Reviews.CreateReviewResponse;

public class ReviewResponseCreatedDomainEventHandler(
    IReviewRepository reviewRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<ReviewResponseCreatedDomainEvent>
{
    public async Task Handle(ReviewResponseCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(notification.ReviewId, cancellationToken);

        if (review is null) return;

        var reviewer = await userRepository.GetByIdAsync(review.UserId, cancellationToken);

        if (reviewer is null) return;

        await emailService.SendAsync(
            reviewer.Email,
            "The owner replied to your review",
            "The apartment owner has responded to the review you left.");
    }
}