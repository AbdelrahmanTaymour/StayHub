using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Reviews.GetReview;

public sealed record GetReviewQuery(Guid ReviewId) : IQuery<ReviewResponse>;