using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Reviews;

namespace StayHub.Application.Reviews.CreateReview;

internal sealed class CreateReviewCommandHandler(
    IBookingRepository bookingRepository,
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null) return Result.Failure<Guid>(BookingErrors.NotFound);

        if (booking.UserId != request.UserId) return Result.Failure<Guid>(ReviewErrors.NotAuthorized);

        var alreadyReviewed = await reviewRepository.ExistsForBookingAsync(request.BookingId, cancellationToken);

        if (alreadyReviewed) return Result.Failure<Guid>(ReviewErrors.AlreadyReviewed);

        var ratingResult = Rating.Create(request.Rating);

        if (ratingResult.IsFailure) return Result.Failure<Guid>(Rating.Invalid);

        var review = Review.Create(
            booking,
            ratingResult.Value,
            new Comment(request.Comment),
            dateTimeProvider.UtcNow);

        if (review.IsFailure) return Result.Failure<Guid>(review.Error);

        reviewRepository.Add(review.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return review.Value.Id;
    }
}