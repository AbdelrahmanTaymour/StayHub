using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Favorites;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class SearchApartmentsTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Search_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_ShouldReturnPagedEnvelope_WithExpectedShape()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search());

        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().BeGreaterThan(0);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        result.TotalPages.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Search_ShouldReturnOnlyActiveApartments_WhenResultsAreRetrieved()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var uniqueCity = $"City{Guid.NewGuid():N}";

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            ApartmentTestData.ValidCreateRequest(city: uniqueCity));

        var apartmentId =
            await createResponse.Content.ReadFromJsonAsync<Guid>();

        var deactivateResponse = await HttpClient.PostAsync(
            ApartmentRoutes.Deactivate(apartmentId),
            null);

        deactivateResponse.EnsureSuccessStatusCode();

        // Reset the cache so the search verifies the active-status filter.
        await Factory.ResetCacheAsync();

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search($"city={uniqueCity}"));

        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenMinPriceIsGreaterThanMaxPrice()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search("minPrice=100&maxPrice=10"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenMinPriceIsNegative()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search("minPrice=-1"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenMaxPriceIsZero()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search("maxPrice=0"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenStartDateIsInThePast()
    {
        // Arrange
        var pastDate = DateOnly
            .FromDateTime(DateTime.UtcNow.AddDays(-5))
            .ToString("yyyy-MM-dd");

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search($"start={pastDate}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var start = DateOnly
            .FromDateTime(DateTime.UtcNow.AddDays(10))
            .ToString("yyyy-MM-dd");

        var end = DateOnly
            .FromDateTime(DateTime.UtcNow.AddDays(5))
            .ToString("yyyy-MM-dd");

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search($"start={start}&end={end}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenStartDateEqualsEndDate()
    {
        // Arrange
        var date = DateOnly
            .FromDateTime(DateTime.UtcNow.AddDays(10))
            .ToString("yyyy-MM-dd");

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search($"start={date}&end={date}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenPageIsLessThanOne()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search("page=0"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenPageSizeExceedsMaximum()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search("pageSize=101"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenPageSizeIsZero()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search("pageSize=0"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_ShouldReturnOnlyMatchingApartments_WhenFilteringByCity()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var targetCity = $"Target{Guid.NewGuid():N}";
        var otherCity = $"Other{Guid.NewGuid():N}";

        await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            ApartmentTestData.ValidCreateRequest(city: targetCity));

        await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            ApartmentTestData.ValidCreateRequest(city: otherCity));

        // Reset the cache to ensure the response is based on the newly created apartments.
        await Factory.ResetCacheAsync();

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Search($"city={targetCity}"));

        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        result!.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(1);

        result.Items[0]
            .GetProperty("city")
            .GetString()
            .Should()
            .Be(targetCity);
    }

    [Fact]
    public async Task Search_ShouldReturnIsFavoritedTrue_WhenCallerFavoritedApartment()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var uniqueCity = $"Fav{Guid.NewGuid():N}";

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            ApartmentTestData.ValidCreateRequest(city: uniqueCity));

        var apartmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var favoriteResponse = await HttpClient.PutAsync(FavoriteRoutes.Add(apartmentId), null);
        favoriteResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.Search($"city={uniqueCity}"));

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        result!.Items.Should().ContainSingle();
        result.Items[0].GetProperty("isFavorited").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Search_ShouldNotLeakFavoriteState_AcrossUsersSharingCachedResults()
    {
        // Arrange: user A favorites an apartment.
        var (accessTokenA, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessTokenA);

        var uniqueCity = $"Leak{Guid.NewGuid():N}";

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            ApartmentTestData.ValidCreateRequest(city: uniqueCity));

        var apartmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        await HttpClient.PutAsync(FavoriteRoutes.Add(apartmentId), null);

        // Reset the cache so the next searches share one cached entry.
        await Factory.ResetCacheAsync();

        var searchUrl = ApartmentRoutes.Search($"city={uniqueCity}");

        // User A searches first, which populates the shared cache entry.
        var userAResponse = await HttpClient.GetAsync(searchUrl);
        var userAResult = await userAResponse.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        userAResult!.Items[0].GetProperty("isFavorited").GetBoolean().Should().BeTrue();

        // User B searches the same query, served from the same cache entry.
        var (accessTokenB, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessTokenB);

        // Act
        var userBResponse = await HttpClient.GetAsync(searchUrl);

        // Assert
        userBResponse.EnsureSuccessStatusCode();

        var userBResult = await userBResponse.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        userBResult!.Items.Should().ContainSingle();
        userBResult.Items[0].GetProperty("isFavorited").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Search_ShouldReturnIsFavoritedFalse_WhenCallerIsAnonymous()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var uniqueCity = $"Anon{Guid.NewGuid():N}";

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            ApartmentTestData.ValidCreateRequest(city: uniqueCity));

        var apartmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        await HttpClient.PutAsync(FavoriteRoutes.Add(apartmentId), null);

        await Factory.ResetCacheAsync();

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.Search($"city={uniqueCity}"));

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        result!.Items.Should().ContainSingle();
        result.Items[0].GetProperty("isFavorited").GetBoolean().Should().BeFalse();
    }

    // Mirrors the shape of PagedResponse<T> as serialized over the wire.
    private sealed record PagedResponseDto<T>(
        List<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}