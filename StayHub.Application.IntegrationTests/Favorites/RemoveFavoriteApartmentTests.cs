using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Favorites.RemoveFavoriteApartment;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Favorites;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Favorites;

public class RemoveFavoriteApartmentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task RemoveFavoriteApartment_ShouldDeleteTheFavorite_WhenItExists()
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
        var result = await Sender.Send(new RemoveFavoriteApartmentCommand(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var stillExists = await DbContext.Set<FavoriteApartment>()
            .AnyAsync(f => f.UserId == user.Id && f.ApartmentId == apartment.Id);
        stillExists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveFavoriteApartment_ShouldReturnNotFound_WhenFavoriteDoesNotExist()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var user = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, user, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new RemoveFavoriteApartmentCommand(apartment.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FavoriteApartmentErrors.NotFound);
    }
}