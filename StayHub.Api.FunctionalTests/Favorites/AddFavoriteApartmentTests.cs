using System.Net;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Favorites;

public sealed class AddFavoriteApartmentTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Add_ShouldReturnNoContent_WhenApartmentExists()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (userToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(userToken);

        // Act
        var response = await HttpClient.PutAsync(FavoriteRoutes.ById(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Add_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PutAsync(FavoriteRoutes.ById(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Add_ShouldReturnConflict_WhenAlreadyFavorited()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (userToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(userToken);
        var firstAdd = await HttpClient.PutAsync(FavoriteRoutes.ById(apartmentId), null);
        firstAdd.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PutAsync(FavoriteRoutes.ById(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("FavoriteApartment.AlreadyFavorited");
    }

    [Fact]
    public async Task Add_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PutAsync(FavoriteRoutes.ById(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Add_ShouldSucceed_WhenOwnerFavoritesTheirOwnApartment()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PutAsync(FavoriteRoutes.ById(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}