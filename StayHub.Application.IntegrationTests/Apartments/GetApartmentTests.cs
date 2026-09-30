using FluentAssertions;
using StayHub.Application.Apartments.GetApartment;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Reviews;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Favorites;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetApartmentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetApartment_ShouldReturnFailure_WhenApartmentIsNotFound()
    {
        // Arrange
        var query = new GetApartmentQuery(Guid.CreateVersion7());

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetApartment_ShouldReturnDetails_WhenApartmentExists()
    {
        // Arrange
        var owner = UserTestData.CreateUser(firstName: "Kenji", lastName: "Takahashi");
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Nile View Studio",
            city: "Cairo",
            priceAmount: 750m,
            priceCurrency: "USD");

        var amenity = Enum.GetValues<Amenity>().First();
        apartment.AddAmenity(amenity);

        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(apartment.Id);
        result.Value.OwnerId.Should().Be(apartment.OwnerId);
        result.Value.Name.Should().Be("Nile View Studio");
        result.Value.PriceAmount.Should().Be(750m);
        result.Value.PriceCurrency.Should().Be("USD");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Address.City.Should().Be("Cairo");
        result.Value.Amenities.Should().ContainSingle().Which.Should().Be(amenity.ToString());
        result.Value.Images.Should().BeEmpty();

        result.Value.Host.Id.Should().Be(owner.Id);
        result.Value.Host.FullName.Should().Be("Kenji Takahashi");
        result.Value.Host.AvatarUrl.Should().BeNull();

        result.Value.Rating.Should().BeNull();
        result.Value.ReviewCount.Should().Be(0);
        result.Value.RecentReviews.Should().BeEmpty();
        result.Value.IsFavorited.Should().BeFalse();
    }

    [Fact]
    public async Task GetApartment_ShouldReturnHostAvatar_WhenHostHasProfile()
    {
        // Arrange
        var owner = UserTestData.CreateUser(firstName: "Kenji", lastName: "Takahashi");
        var profile = UserTestData.CreateProfile(owner.Id);
        profile.UpdateAvatar(new AvatarUrl("https://test-storage.local/host-avatar.png"), DateTime.UtcNow);

        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, profile, apartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Host.AvatarUrl.Should().Be("https://test-storage.local/host-avatar.png");
    }

    [Fact]
    public async Task GetApartment_ShouldReturnImagesInDisplayOrder_WhenApartmentHasMultipleImages()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);

        var secondImage = ApartmentTestData.CreateImage(apartment.Id, displayOrder: 1);
        var firstImage = ApartmentTestData.CreateImage(apartment.Id, displayOrder: 0, isPrimary: true);

        // Deliberately added out of order to prove ORDER BY display_order, not insertion order.
        DbContext.Add(secondImage);
        DbContext.Add(firstImage);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Images.Should().HaveCount(2);
        result.Value.Images[0].Id.Should().Be(firstImage.Id);
        result.Value.Images[0].IsPrimary.Should().BeTrue();
        result.Value.Images[1].Id.Should().Be(secondImage.Id);
        result.Value.Images[1].IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task GetApartment_ShouldReturnAggregateRatingAndRecentReviews_WhenReviewsExist()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser(firstName: "Sarah", lastName: "Jenkins");
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

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(4.0); // average of 3 and 5
        result.Value.ReviewCount.Should().Be(2);
        result.Value.RecentReviews.Should().HaveCount(2);
        result.Value.RecentReviews[0].Id.Should().Be(newerReview.Id); // most recent first
        result.Value.RecentReviews[1].Id.Should().Be(olderReview.Id);
    }

    [Fact]
    public async Task GetApartment_ShouldReturnNotFound_WhenApartmentIsInactive_AndCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        apartment.Deactivate();
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetApartment_ShouldReturnDetails_WhenApartmentIsInactive_AndCallerIsTheOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        apartment.Deactivate();
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetApartment_ShouldReturnDetails_WhenApartmentIsInactive_AndCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        apartment.Deactivate();
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetApartment_ShouldReturnIsFavoritedTrue_WhenCurrentUserFavoritedIt()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var favoritingUser = UserTestData.CreateUser();
        var favorite = FavoriteApartment.Create(favoritingUser.Id, apartment.Id, DateTime.UtcNow);

        DbContext.AddRange(owner, apartment, favoritingUser, favorite);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(favoritingUser.Id, Role.Guest.Name);

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFavorited.Should().BeTrue();
    }
}