using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Reviews;

namespace StayHub.Application.Reviews.CreateReviewResponse;

internal sealed class CreateReviewResponseCommandHandler(
    IReviewRepository reviewRepository,
    IReviewResponseRepository reviewResponseRepository,
    IApartmentRepository apartmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateReviewResponseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateReviewResponseCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);

        if (review is null) return Result.Failure<Guid>(ReviewErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(review.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

        if (apartment.OwnerId != userContext.UserId) return Result.Failure<Guid>(ApartmentErrors.NotAuthorized);

        var existingResponse = await reviewResponseRepository.GetByReviewIdAsync(request.ReviewId, cancellationToken);

        if (existingResponse is not null) return Result.Failure<Guid>(ReviewResponseErrors.AlreadyRespondedTo);

        var response = ReviewResponse.Create(request.ReviewId, new Comment(request.Comment), dateTimeProvider.UtcNow);

        reviewResponseRepository.Add(response);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response.Id;
    }
}