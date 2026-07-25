using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Reviews.CreateReview;

public sealed record CreateReviewCommand(Guid BookingId, Guid UserId, int Rating, string Comment) : ICommand<Guid>;