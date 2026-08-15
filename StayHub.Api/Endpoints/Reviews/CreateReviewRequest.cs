namespace StayHub.Api.Endpoints.Reviews;

public sealed record CreateReviewRequest(Guid BookingId, int Rating, string Comment);