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
        result.Value.Items[0].NightsStayed.Should().Be(4);
        result.Value.Items[1].Id.Should().Be(olderReview.Id);
        result.Value.Items[1].OwnerResponseComment.Should().Be("Thanks!");
        result.Value.Items[1].NightsStayed.Should().Be(4);
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

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(r => r.Id == reviews[0].Id);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetReviewsByApartment_ShouldFilterByResponseStatus_NeedsResponse()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var respondedBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService, baseTime);
        respondedBooking.Confirm(baseTime);
        respondedBooking.Complete(baseTime);

        var unrespondedBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 5), PricingService,
            baseTime.AddMinutes(1));
        unrespondedBooking.Confirm(baseTime.AddMinutes(1));
        unrespondedBooking.Complete(baseTime.AddMinutes(1));

        DbContext.AddRange(respondedBooking, unrespondedBooking);
        await DbContext.SaveChangesAsync();

        var respondedReview = ReviewTestData.CreateReview(respondedBooking, rating: 4, utcNow: baseTime);
        var unrespondedReview =
            ReviewTestData.CreateReview(unrespondedBooking, rating: 4, utcNow: baseTime.AddMinutes(1));
        DbContext.AddRange(respondedReview, unrespondedReview);
        await DbContext.SaveChangesAsync();

        var response = ReviewResponse.Create(respondedReview.Id, new Comment("Thanks!"), baseTime.AddMinutes(2));
        DbContext.Add(response);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentReviewsQuery(
            apartment.Id, ResponseStatus: ReviewResponseStatusFilter.NeedsResponse, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(r => r.Id == unrespondedReview.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetReviewsByApartment_ShouldFilterByResponseStatus_Responded()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var respondedBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService, baseTime);
        respondedBooking.Confirm(baseTime);
        respondedBooking.Complete(baseTime);

        var unrespondedBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 5), PricingService,
            baseTime.AddMinutes(1));
        unrespondedBooking.Confirm(baseTime.AddMinutes(1));
        unrespondedBooking.Complete(baseTime.AddMinutes(1));

        DbContext.AddRange(respondedBooking, unrespondedBooking);
        await DbContext.SaveChangesAsync();

        var respondedReview = ReviewTestData.CreateReview(respondedBooking, rating: 4, utcNow: baseTime);
        var unrespondedReview =
            ReviewTestData.CreateReview(unrespondedBooking, rating: 4, utcNow: baseTime.AddMinutes(1));
        DbContext.AddRange(respondedReview, unrespondedReview);
        await DbContext.SaveChangesAsync();

        var response = ReviewResponse.Create(respondedReview.Id, new Comment("Thanks!"), baseTime.AddMinutes(2));
        DbContext.Add(response);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentReviewsQuery(
            apartment.Id, ResponseStatus: ReviewResponseStatusFilter.Responded, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(r => r.Id == respondedReview.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Theory]
    [InlineData(ReviewRatingFilter.FiveStars, 5)]
    [InlineData(ReviewRatingFilter.FourStars, 4)]
    public async Task GetReviewsByApartment_ShouldFilterByExactRating(ReviewRatingFilter filter, int expectedRating)
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;
        var ratings = new[] { 5, 4, 3, 2, 1 };

        var bookings = ratings
            .Select((_, i) => BookingTestData.Reserve(
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
            .Select((b, i) => ReviewTestData.CreateReview(b, rating: ratings[i], utcNow: baseTime.AddMinutes(i)))
            .ToList();

        DbContext.AddRange(reviews);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentReviewsQuery(
            apartment.Id, Rating: filter, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(r => r.Rating == expectedRating);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetReviewsByApartment_ShouldFilterByRating_ThreeStarsOrLess()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;
        var ratings = new[] { 5, 4, 3, 2, 1 };

        var bookings = ratings
            .Select((_, i) => BookingTestData.Reserve(
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
            .Select((b, i) => ReviewTestData.CreateReview(b, rating: ratings[i], utcNow: baseTime.AddMinutes(i)))
            .ToList();

        DbContext.AddRange(reviews);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentReviewsQuery(
            apartment.Id, Rating: ReviewRatingFilter.ThreeStarsOrLess, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(r => r.Rating <= 3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetReviewsByApartment_ShouldSortByOldestFirst_WhenRequested()
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

        var newerBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 5), PricingService,
            baseTime.AddMinutes(1));
        newerBooking.Confirm(baseTime.AddMinutes(1));
        newerBooking.Complete(baseTime.AddMinutes(1));

        DbContext.AddRange(olderBooking, newerBooking);
        await DbContext.SaveChangesAsync();

        var olderReview = ReviewTestData.CreateReview(olderBooking, rating: 3, utcNow: baseTime);
        var newerReview = ReviewTestData.CreateReview(newerBooking, rating: 5, utcNow: baseTime.AddMinutes(1));
        DbContext.AddRange(olderReview, newerReview);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentReviewsQuery(
            apartment.Id, SortOrder: ReviewSortOrder.Oldest, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Id.Should().Be(olderReview.Id);
        result.Value.Items[1].Id.Should().Be(newerReview.Id);
    }

    [Fact]
    public async Task GetReviewsByApartment_ShouldSortByRatingDescending_WhenRequested()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var lowRatedBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService, baseTime);
        lowRatedBooking.Confirm(baseTime);
        lowRatedBooking.Complete(baseTime);

        var highRatedBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 5), PricingService,
            baseTime.AddMinutes(1));
        highRatedBooking.Confirm(baseTime.AddMinutes(1));
        highRatedBooking.Complete(baseTime.AddMinutes(1));

        DbContext.AddRange(lowRatedBooking, highRatedBooking);
        await DbContext.SaveChangesAsync();

        var lowRatedReview = ReviewTestData.CreateReview(lowRatedBooking, rating: 2, utcNow: baseTime.AddMinutes(2));
        var highRatedReview = ReviewTestData.CreateReview(highRatedBooking, rating: 5, utcNow: baseTime);
        DbContext.AddRange(lowRatedReview, highRatedReview);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentReviewsQuery(
            apartment.Id, SortOrder: ReviewSortOrder.RatingDesc, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Id.Should().Be(highRatedReview.Id);
        result.Value.Items[1].Id.Should().Be(lowRatedReview.Id);
    }
}