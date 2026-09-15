namespace StayHub.Application.Reviews.CreateReview;

public sealed record ReviewReceivedNotificationPayload(
    Guid ReviewId,
    Guid ApartmentId,
    int Rating,
    string Message);