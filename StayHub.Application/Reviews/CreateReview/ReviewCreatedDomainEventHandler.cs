using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Notifications;
using StayHub.Domain.Reviews;
using StayHub.Domain.Reviews.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Reviews.CreateReview;

public class ReviewCreatedDomainEventHandler(
    IReviewRepository reviewRepository,
    IApartmentRepository apartmentRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<ReviewCreatedDomainEvent>
{
    public async Task Handle(ReviewCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(domainEvent.ReviewId, cancellationToken);

        if (review is null) return;

        var apartment = await apartmentRepository.GetByIdAsync(review.ApartmentId, cancellationToken);

        if (apartment is null) return;

        var owner = await userRepository.GetByIdAsync(apartment.OwnerId, cancellationToken);

        if (owner is null) return;

        var payload = new ReviewReceivedNotificationPayload(
            ReviewId: review.Id,
            ApartmentId: review.ApartmentId,
            Rating: review.Rating.Value,
            Message: review.Comment.Value);

        var notification = Notification.Create(
            owner.Id,
            NotificationType.ReviewReceived,
            JsonSerializer.Serialize(payload),
            dateTimeProvider.UtcNow);

        notificationRepository.Add(notification);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(
            owner.Email,
            "You received a new review",
            $"Your apartment received a {review.Rating}-star review.");
    }
}