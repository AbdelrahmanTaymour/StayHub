using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Reviews;

public static class ReviewResponseErrors
{
    public static Error NotFound = new(
        "ReviewResponse.NotFound",
        "The response with the specified identifier was not found");

    public static Error AlreadyRespondedTo = new(
        "ReviewResponse.AlreadyRespondedTo",
        "This review has already received a response");
}