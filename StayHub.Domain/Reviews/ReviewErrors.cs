using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Reviews;

public static class ReviewErrors
{
    public static readonly Error NotEligible = Error.Validation(
        "Review.NotEligible",
        "The review is not eligible because the booking is not yet completed");

    public static readonly Error NotFound = Error.NotFound(
        "Review.NotFound",
        "The review with the specified identifier was not found");

    public static readonly Error AlreadyReviewed = Error.Conflict(
        "Review.AlreadyReviewed",
        "This booking has already been reviewed");

    public static readonly Error NotAuthorized = Error.Forbidden(
        "Review.NotAuthorized",
        "Only the guest who made this booking can review it");
}