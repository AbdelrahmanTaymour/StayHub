namespace StayHub.Application.Reviews.CreateReviewResponse;

public sealed record ReviewResponseCreatedNotificationPayload(
    Guid ReviewId,
    Guid ResponseReviewId,
    string Message);