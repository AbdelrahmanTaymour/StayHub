using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Reviews.CreateReviewResponse;

public sealed record CreateReviewResponseCommand(Guid ReviewId, string Comment)
    : ICommand<Guid>;