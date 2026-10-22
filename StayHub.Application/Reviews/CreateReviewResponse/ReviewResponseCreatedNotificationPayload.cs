namespace StayHub.Application.Reviews.CreateReviewResponse;

public sealed record ReviewResponseCreatedNotificationPayload(
    Guid ReviewId,
    Guid ApartmentId,
    Guid ResponseReviewId,
    string Message);