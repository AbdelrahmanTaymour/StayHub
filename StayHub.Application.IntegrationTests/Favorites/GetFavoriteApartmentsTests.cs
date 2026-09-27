using FluentAssertions;
using StayHub.Application.Favorites.GetFavoriteApartments;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Reviews;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Favorites;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Favorites;

public class GetFavoriteApartmentsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetFavoriteApartments_ShouldReturnEmptyPage_WhenUserHasNoFavorites()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetFavoriteApartmentsQuery(Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFavoriteApartments_ShouldReturnOnlyThatUsersFavorites_OrderedByCreatedOnDescending()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var user = UserTestData.CreateUser();
        var otherUser = UserTestData.CreateUser();
        var apartmentA = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment A");
        var apartmentB = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment B");
        DbContext.AddRange(owner, user, otherUser, apartmentA, apartmentB);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;
        var olderFavorite = FavoriteApartment.Create(user.Id, apartmentA.Id, baseTime);
        var newerFavorite = FavoriteApartment.Create(user.Id, apartmentB.Id, baseTime.AddMinutes(1));
        var otherUsersFavorite = FavoriteApartment.Create(otherUser.Id, apartmentA.Id, baseTime);
        DbContext.AddRange(olderFavorite, newerFavorite, otherUsersFavorite);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetFavoriteApartmentsQuery(Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].ApartmentId.Should().Be(apartmentB.Id);
        result.Value.Items[1].ApartmentId.Should().Be(apartmentA.Id);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetFavoriteApartments_ShouldReturnMappedPriceAndCity()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var user = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id, name: "Nile View Studio", city: "Cairo", priceAmount: 300m, priceCurrency: "USD");
        DbContext.AddRange(owner, user, apartment);
        await DbContext.SaveChangesAsync();

        var favorite = FavoriteApartment.Create(user.Id, apartment.Id, DateTime.UtcNow);
        DbContext.Add(favorite);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetFavoriteApartmentsQuery(Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.Should().ContainSingle().Which;
        item.City.Should().Be("Cairo");
        item.PricePerNight.Should().Be(300m);
        item.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetFavoriteApartments_ShouldReturnAggregateRating_WhenReviewsExist()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var favoritingUser = UserTestData.CreateUser();
        var reviewer = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, favoritingUser, reviewer, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;
        var booking = BookingTestData.Reserve(
            apartment, reviewer.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService, baseTime);
        booking.Confirm(baseTime);
        booking.Complete(baseTime);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var review = ReviewTestData.CreateReview(booking, rating: 5, utcNow: baseTime);
        var favorite = FavoriteApartment.Create(favoritingUser.Id, apartment.Id, baseTime);
        DbContext.AddRange(review, favorite);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(favoritingUser.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetFavoriteApartmentsQuery(Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.Should().ContainSingle().Which;
        item.Rating.Should().Be(5.0);
        item.ReviewCount.Should().Be(1);
    }

    [Fact]
    public async Task GetFavoriteApartments_ShouldReturnNullRating_WhenNoReviewsExist()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var user = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, user, apartment);
        await DbContext.SaveChangesAsync();

        var favorite = FavoriteApartment.Create(user.Id, apartment.Id, DateTime.UtcNow);
        DbContext.Add(favorite);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetFavoriteApartmentsQuery(Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.Should().ContainSingle().Which;
        item.Rating.Should().BeNull();
        item.ReviewCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFavoriteApartments_ShouldExcludeInactiveApartments()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var user = UserTestData.CreateUser();
        var activeApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Active");
        var inactiveApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Inactive");
        inactiveApartment.Deactivate();
        DbContext.AddRange(owner, user, activeApartment, inactiveApartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;
        var activeFavorite = FavoriteApartment.Create(user.Id, activeApartment.Id, baseTime);
        var inactiveFavorite = FavoriteApartment.Create(user.Id, inactiveApartment.Id, baseTime.AddMinutes(1));
        DbContext.AddRange(activeFavorite, inactiveFavorite);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetFavoriteApartmentsQuery(Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(a => a.ApartmentId == activeApartment.Id);
    }

    [Fact]
    public async Task GetFavoriteApartments_ShouldRespectPagination()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var user = UserTestData.CreateUser();
        var baseTime = DateTime.UtcNow;

        var apartments = Enumerable.Range(0, 3)
            .Select(i => ApartmentTestData.CreateApartment(ownerId: owner.Id, name: $"Apartment {i}"))
            .ToList();

        DbContext.Add(owner);
        DbContext.Add(user);
        DbContext.AddRange(apartments);
        await DbContext.SaveChangesAsync();

        var favorites = apartments
            .Select((a, i) => FavoriteApartment.Create(user.Id, a.Id, baseTime.AddMinutes(i)))
            .ToList();
        DbContext.AddRange(favorites);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        var query = new GetFavoriteApartmentsQuery(Page: 2, PageSize: 2);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(a => a.ApartmentId == apartments[0].Id);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
    }
}