using FluentValidation;

namespace StayHub.Application.Reviews.CreateReviewResponse;

public class CreateReviewResponseCommandValidator : AbstractValidator<CreateReviewResponseCommand>
{
    public CreateReviewResponseCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
    }
}