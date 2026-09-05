using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Reviews;

public static class ReviewResponseErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "ReviewResponse.NotFound",
        "The response with the specified identifier was not found");

    public static readonly Error AlreadyRespondedTo = Error.Conflict(
        "ReviewResponse.AlreadyRespondedTo",
        "This review has already received a response");

    public static readonly Error NotAuthorized = Error.Forbidden(
        "ReviewResponse.NotAuthorized",
        "You can only respond to your own apartment's reviews"
    );
}