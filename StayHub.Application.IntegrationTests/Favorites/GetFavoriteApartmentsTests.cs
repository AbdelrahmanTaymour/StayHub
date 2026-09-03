using FluentAssertions;
using StayHub.Application.Favorites.GetFavoriteApartments;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Favorites;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Favorites;

public class GetFavoriteApartmentsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetFavoriteApartments_ShouldReturnEmpty_WhenUserHasNoFavorites()
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
        result.Value.Should().BeEmpty();
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
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(apartmentB.Id);
        result.Value[1].Id.Should().Be(apartmentA.Id);
    }
}