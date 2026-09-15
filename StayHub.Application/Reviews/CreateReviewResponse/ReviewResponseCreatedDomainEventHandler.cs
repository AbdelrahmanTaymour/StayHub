using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Notifications;
using StayHub.Domain.Reviews;
using StayHub.Domain.Reviews.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Reviews.CreateReviewResponse;

public class ReviewResponseCreatedDomainEventHandler(
    IReviewRepository reviewRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<ReviewResponseCreatedDomainEvent>
{
    public async Task Handle(ReviewResponseCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(domainEvent.ReviewId, cancellationToken);

        if (review is null) return;

        var reviewer = await userRepository.GetByIdAsync(review.UserId, cancellationToken);

        if (reviewer is null) return;

        var payload = new ReviewResponseCreatedNotificationPayload(
            ReviewId: domainEvent.ReviewId,
            ResponseReviewId: domainEvent.ReviewResponseId,
            Message: review.Comment.Value);

        var notification = Notification.Create(
            reviewer.Id,
            NotificationType.ReviewResponseReceived,
            JsonSerializer.Serialize(payload),
            dateTimeProvider.UtcNow);

        notificationRepository.Add(notification);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(
            reviewer.Email,
            "The owner replied to your review",
            "The apartment owner has responded to the review you left.");
    }
}