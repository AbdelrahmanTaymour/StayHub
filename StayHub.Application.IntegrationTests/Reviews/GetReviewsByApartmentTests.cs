using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Reviews.GetReviewsByApartment;
using StayHub.Domain.Reviews;

namespace StayHub.Application.IntegrationTests.Reviews;

public class GetReviewsByApartmentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetReviewsByApartment_ShouldReturnEmpty_WhenNoReviewsExist()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetReviewsByApartmentQuery(apartment.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task
        GetReviewsByApartment_ShouldReturnReviews_OrderedByCreatedOnDescending_WithOwnerResponseWhereItExists()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var olderBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService, baseTime);
        olderBooking.Confirm(baseTime);
        olderBooking.Complete(baseTime);
        DbContext.Add(olderBooking);
        await DbContext.SaveChangesAsync();

        var newerBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 5), PricingService,
            baseTime.AddMinutes(1));
        newerBooking.Confirm(baseTime.AddMinutes(1));
        newerBooking.Complete(baseTime.AddMinutes(1));
        DbContext.Add(newerBooking);
        await DbContext.SaveChangesAsync();

        var olderReview = ReviewTestData.CreateReview(olderBooking, rating: 3, utcNow: baseTime);
        var newerReview = ReviewTestData.CreateReview(newerBooking, rating: 5, utcNow: baseTime.AddMinutes(1));
        DbContext.AddRange(olderReview, newerReview);
        await DbContext.SaveChangesAsync();

        var response = ReviewResponse.Create(olderReview.Id, new Comment("Thanks!"), baseTime.AddMinutes(2));
        DbContext.Add(response);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetReviewsByApartmentQuery(apartment.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(newerReview.Id);
        result.Value[0].OwnerResponseComment.Should().BeNull();
        result.Value[1].Id.Should().Be(olderReview.Id);
        result.Value[1].OwnerResponseComment.Should().Be("Thanks!");
    }
}