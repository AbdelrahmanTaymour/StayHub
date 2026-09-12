namespace StayHub.Api.FunctionalTests.Reviews;

internal static class ReviewTestData
{
    public static object ValidCreateRequest(Guid bookingId, int rating = 5, string? comment = null)
    {
        return new
        {
            BookingId = bookingId,
            Rating = rating,
            Comment = comment ?? "Wonderful stay, would book again."
        };
    }

    public static object ValidResponseRequest(string? comment = null)
    {
        return new { Comment = comment ?? "Thank you for staying with us!" };
    }
}