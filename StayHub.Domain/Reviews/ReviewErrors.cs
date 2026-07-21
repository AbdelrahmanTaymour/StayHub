using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Reviews;

public static class ReviewErrors
{
    public static readonly Error NotEligible = new(
        "Review.NotEligible",
        "The review is not eligible because the booking is not yet completed");

    public static Error NotFound = new(
        "Review.NotFound",
        "The review with the specified identifier was not found");

    public static Error AlreadyReviewed = new(
        "Review.AlreadyReviewed",
        "This booking has already been reviewed");

    public static Error NotAuthorized = new(
        "Review.NotAuthorized",
        "Only the guest who made this booking can review it");

    public static Error UnExpectedError = new(
        "Review.UnExpectedError",
        "Something went wrong while processing your review. Please try again later.");
}