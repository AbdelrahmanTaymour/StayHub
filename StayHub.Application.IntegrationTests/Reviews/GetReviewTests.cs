using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Reviews.GetReview;
using StayHub.Domain.Reviews;
using ReviewResponse = StayHub.Domain.Reviews.ReviewResponse;

namespace StayHub.Application.IntegrationTests.Reviews;

public class GetReviewTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetReview_ShouldReturnNotFound_WhenReviewDoesNotExist()
    {
        // Act
        var result = await Sender.Send(new GetReviewQuery(Guid.CreateVersion7()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.NotFound);
    }

    [Fact]
    public async Task GetReview_ShouldReturnNullOwnerResponse_WhenNoResponseExists()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService);
        booking.Confirm(DateTime.UtcNow);
        booking.Complete(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var review = ReviewTestData.CreateReview(booking, rating: 4);
        DbContext.Add(review);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetReviewQuery(review.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(4);
        result.Value.OwnerResponseComment.Should().BeNull();
    }

    [Fact]
    public async Task GetReview_ShouldReturnOwnerResponseComment_WhenResponseExists()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService);
        booking.Confirm(DateTime.UtcNow);
        booking.Complete(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var review = ReviewTestData.CreateReview(booking);
        DbContext.Add(review);
        await DbContext.SaveChangesAsync();

        var response = ReviewResponse.Create(review.Id, new Comment("Thanks for staying!"), DateTime.UtcNow);
        DbContext.Add(response);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetReviewQuery(review.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OwnerResponseComment.Should().Be("Thanks for staying!");
    }
}