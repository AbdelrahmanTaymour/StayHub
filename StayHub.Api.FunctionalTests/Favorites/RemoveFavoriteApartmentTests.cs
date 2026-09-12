using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Favorites;

public sealed class RemoveFavoriteApartmentTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Remove_ShouldReturnNoContent_WhenFavoriteExists()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (userToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(userToken);
        var addResponse = await HttpClient.PutAsync(FavoriteRoutes.ById(apartmentId), null);
        addResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.DeleteAsync(FavoriteRoutes.ById(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Remove_ShouldReturnNotFound_WhenNotCurrentlyFavorited()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (userToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(userToken);

        // Act
        var response = await HttpClient.DeleteAsync(FavoriteRoutes.ById(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("FavoriteApartment.NotFound");
    }

    [Fact]
    public async Task Remove_ShouldReturnNotFound_WhenApartmentIdIsRandomGuid()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.DeleteAsync(FavoriteRoutes.ById(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Remove_ShouldOnlyAffectCallersOwnFavorite_NotAnotherUsersFavoriteOfSameApartment()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (userAToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(userAToken);
        var addAResponse = await HttpClient.PutAsync(FavoriteRoutes.ById(apartmentId), null);
        addAResponse.EnsureSuccessStatusCode();

        var (userBToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(userBToken);

        // Act
        var response = await HttpClient.DeleteAsync(FavoriteRoutes.ById(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // userB never favorited it

        AuthenticateAs(userAToken);
        var getResponse = await HttpClient.GetAsync(FavoriteRoutes.Get());
        getResponse.EnsureSuccessStatusCode();
        var results = await getResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().ContainSingle(); // userA's favorite is untouched
    }

    [Fact]
    public async Task Remove_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.DeleteAsync(FavoriteRoutes.ById(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}