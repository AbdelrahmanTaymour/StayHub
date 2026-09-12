using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Favorites;

public sealed class GetFavoriteApartmentsTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.GetAsync(FavoriteRoutes.Get());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldReturnEmptyList_WhenCallerHasNoFavorites()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(FavoriteRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnOnlyCallersOwnFavorites()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (userAToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(userAToken);
        var addAResponse = await HttpClient.PutAsync(FavoriteRoutes.ById(apartmentId), null);
        addAResponse.EnsureSuccessStatusCode();

        var (userBToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(userBToken);

        // Act
        var response = await HttpClient.GetAsync(FavoriteRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }
}