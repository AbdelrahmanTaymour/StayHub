using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Apartments;
using StayHub.Domain.Reviews;
using StayHub.Domain.Reviews.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Reviews.CreateReview;

public class ReviewCreatedDomainEventHandler(
    IReviewRepository reviewRepository,
    IApartmentRepository apartmentRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<ReviewCreatedDomainEvent>
{
    public async Task Handle(ReviewCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(notification.ReviewId, cancellationToken);

        if (review is null) return;

        var apartment = await apartmentRepository.GetByIdAsync(review.ApartmentId, cancellationToken);

        if (apartment is null) return;

        var owner = await userRepository.GetByIdAsync(apartment.OwnerId, cancellationToken);

        if (owner is null) return;

        await emailService.SendAsync(
            owner.Email,
            "You received a new review",
            $"Your apartment received a {review.Rating}-star review.");
    }
}