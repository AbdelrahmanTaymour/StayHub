using FluentAssertions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Reviews;
using StayHub.Domain.Reviews.Events;
using StayHub.Domain.UnitTests.Bookings;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Reviews;

public class ReviewTests : BaseTest
{
    private static readonly Rating Rating = Rating.Create(5).Value;
    private static readonly Comment Comment = new("Great stay, would book again!");

    [Fact]
    public void Create_Should_ReturnSuccessAndSetPropertyValues_WhenBookingIsCompleted()
    {
        // Arrange
        var booking = BookingData.CompletedBooking();

        // Act
        var result = Review.Create(booking, Rating, Comment, DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApartmentId.Should().Be(booking.ApartmentId);
        result.Value.BookingId.Should().Be(booking.Id);
        result.Value.UserId.Should().Be(booking.UserId);
        result.Value.Rating.Should().Be(Rating);
        result.Value.Comment.Should().Be(Comment);
    }

    [Fact]
    public void Create_Should_RaiseReviewCreatedDomainEvent_WhenBookingIsCompleted()
    {
        // Arrange
        var booking = BookingData.CompletedBooking();

        // Act
        var review = Review.Create(booking, Rating, Comment, DateTime.UtcNow).Value;

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ReviewCreatedDomainEvent>(review);
        domainEvent.ReviewId.Should().Be(review.Id);
        domainEvent.ApartmentId.Should().Be(booking.ApartmentId);
    }

    [Theory]
    [InlineData(BookingStatus.Reserved)]
    [InlineData(BookingStatus.Confirmed)]
    public void Create_Should_ReturnFailure_WhenBookingIsNotCompleted(BookingStatus status)
    {
        // Arrange
        var booking = status == BookingStatus.Reserved
            ? BookingData.Reserve()
            : BookingData.ReserveAndConfirm();

        // Act
        var result = Review.Create(booking, Rating, Comment, DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.NotEligible);
    }

    [Fact]
    public void Create_Should_ReturnFailure_WhenBookingWasCancelled()
    {
        // Arrange — a canceled stay was never actually lived in; reviewing
        // it makes no sense, and NotEligible covers this the same as any
        // other non-Completed status.
        var booking = BookingData.ReserveAndConfirm();
        booking.Cancel(new DateTime(2025, 12, 31));

        // Act
        var result = Review.Create(booking, Rating, Comment, DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.NotEligible);
    }
}