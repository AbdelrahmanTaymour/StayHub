namespace StayHub.Api.Controllers.Reviews;

public sealed record CreateReviewRequest(Guid BookingId, int Rating, string Comment);