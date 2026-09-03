using StayHub.Domain.Bookings;
using StayHub.Domain.Reviews;

namespace StayHub.Application.IntegrationTests.Reviews;

internal static class ReviewTestData
{
    public static Review CreateReview(
        Booking completedBooking,
        int rating = 5,
        string comment = "Great stay!",
        DateTime? utcNow = null)
    {
        var ratingResult = Rating.Create(rating);

        return Review.Create(completedBooking, ratingResult.Value, new Comment(comment), utcNow ?? DateTime.UtcNow)
            .Value;
    }
}