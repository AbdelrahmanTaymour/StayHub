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
    public async Task Get_ShouldReturnEmptyPagedEnvelope_WhenCallerHasNoFavorites()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(FavoriteRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
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
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnFavoritedApartment_WithExpectedPropertyNames()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (userToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(userToken);
        var addResponse = await HttpClient.PutAsync(FavoriteRoutes.ById(apartmentId), null);
        addResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(FavoriteRoutes.Get());
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();

        var item = result.Items[0];
        item.GetProperty("apartmentId").GetGuid().Should().Be(apartmentId);
        item.TryGetProperty("pricePerNight", out _).Should().BeTrue();
        item.TryGetProperty("currency", out _).Should().BeTrue();
        item.TryGetProperty("city", out _).Should().BeTrue();
        item.GetProperty("rating").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("reviewCount").GetInt32().Should().Be(0);
    }

    private sealed record PagedResponseDto<T>(
        List<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}