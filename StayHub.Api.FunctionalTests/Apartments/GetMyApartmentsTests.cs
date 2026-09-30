using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class GetMyApartmentsTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetMine_ShouldReturnUnauthorized_WhenCallerIsAnonymous()
    {
        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.Mine());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMine_ShouldReturnOk_WhenCallerIsAuthenticated()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.Mine());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMine_ShouldReturnPagedEnvelope_WithExpectedShape()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.Mine());

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
    public async Task GetMine_ShouldReturnOnlyCurrentOwnersApartments()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var ownerApartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (otherOwnerToken, _, _) = await RegisterAndAuthenticateAsync();

        AuthenticateAs(otherOwnerToken);

        var otherApartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.Mine());

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        // Assert
        result!.Items.Should().ContainSingle();

        result.Items[0].GetProperty("id").GetGuid().Should().Be(ownerApartmentId);

        result.Items.Should().NotContain(item =>
            item.GetProperty("id").GetGuid() == otherApartmentId);
    }

    [Fact]
    public async Task GetMine_ShouldFilterByActiveStatus()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var activeApartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var inactiveApartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var deactivateResponse = await HttpClient.PostAsync(ApartmentRoutes.Deactivate(inactiveApartmentId), null);

        deactivateResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.Mine("status=Active"));

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        // Assert
        result!.Items.Should().ContainSingle();

        result.Items[0].GetProperty("id").GetGuid().Should().Be(activeApartmentId);
    }

    [Fact]
    public async Task GetMine_ShouldFilterByInactiveStatus()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var inactiveApartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var deactivateResponse = await HttpClient.PostAsync(
            ApartmentRoutes.Deactivate(inactiveApartmentId),
            null);

        deactivateResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Mine("status=Inactive"));

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        // Assert
        result!.Items.Should().ContainSingle();
        result.Items[0].GetProperty("id").GetGuid().Should().Be(inactiveApartmentId);
    }

    [Fact]
    public async Task GetMine_ShouldFilterBySearchTerm()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var targetCity = $"Target{Guid.NewGuid():N}";
        var otherCity = $"Other{Guid.NewGuid():N}";

        await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            ApartmentTestData.ValidCreateRequest(
                city: targetCity));

        await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            ApartmentTestData.ValidCreateRequest(
                city: otherCity));

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.Mine(
            $"search={Uri.EscapeDataString(targetCity)}"));

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        // Assert
        result!.Items.Should().ContainSingle();
        result.Items[0].GetProperty("city").GetString().Should().Be(targetCity);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMine_ShouldRespectPagination()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        for (var i = 0; i < 3; i++)
            await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Mine("page=2&pageSize=2"));

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();

        // Assert
        result!.Items.Should().ContainSingle();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(2);
    }

    private sealed record PagedResponseDto<T>(
        List<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}