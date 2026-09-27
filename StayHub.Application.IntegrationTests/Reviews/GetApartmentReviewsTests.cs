using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Reviews.GetApartmentReviews;
using StayHub.Domain.Reviews;

namespace StayHub.Application.IntegrationTests.Reviews;

public class GetApartmentReviewsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetReviewsByApartment_ShouldReturnEmptyPage_WhenNoReviewsExist()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentReviewsQuery(apartment.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
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
        var result = await Sender.Send(new GetApartmentReviewsQuery(apartment.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be(newerReview.Id);
        result.Value.Items[0].OwnerResponseComment.Should().BeNull();
        result.Value.Items[1].Id.Should().Be(olderReview.Id);
        result.Value.Items[1].OwnerResponseComment.Should().Be("Thanks!");
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetReviewsByApartment_ShouldRespectPagination()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var bookings = Enumerable.Range(0, 3)
            .Select(i => BookingTestData.Reserve(
                apartment, guest.Id,
                new DateOnly(2026, 1, 1).AddDays(i * 10), new DateOnly(2026, 1, 5).AddDays(i * 10),
                PricingService, baseTime.AddMinutes(i)))
            .ToList();

        foreach (var booking in bookings)
        {
            booking.Confirm(baseTime);
            booking.Complete(baseTime);
        }

        DbContext.AddRange(bookings);
        await DbContext.SaveChangesAsync();

        var reviews = bookings
            .Select((b, i) => ReviewTestData.CreateReview(b, rating: 4, utcNow: baseTime.AddMinutes(i)))
            .ToList();

        DbContext.AddRange(reviews);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentReviewsQuery(apartment.Id, Page: 2, PageSize: 2);

        // Act
        var result = await Sender.Send(query);

        // Assert — 3 total, page size 2: page 2 gets the oldest (last by created_on_utc DESC).
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(r => r.Id == reviews[0].Id);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
    }
}