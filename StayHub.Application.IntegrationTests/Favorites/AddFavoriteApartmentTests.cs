using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Favorites.AddFavoriteApartment;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Favorites;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Favorites;

public class AddFavoriteApartmentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task AddFavoriteApartment_ShouldPersistFavorite_WhenNotAlreadyFavorited()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var user = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, user, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new AddFavoriteApartmentCommand(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Set<FavoriteApartment>()
            .SingleAsync(f => f.UserId == user.Id && f.ApartmentId == apartment.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task AddFavoriteApartment_ShouldReturnAlreadyFavorited_WhenFavoriteAlreadyExists()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var user = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, user, apartment);
        await DbContext.SaveChangesAsync();

        var existingFavorite = FavoriteApartment.Create(user.Id, apartment.Id, DateTime.UtcNow);
        DbContext.Add(existingFavorite);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new AddFavoriteApartmentCommand(apartment.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FavoriteApartmentErrors.AlreadyFavorited);
    }
}