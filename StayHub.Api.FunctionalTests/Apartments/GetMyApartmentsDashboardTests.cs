using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class MyApartmentsDashboardTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<JsonElement> GetDashboardAsync(string? query = null)
    {
        var response = await HttpClient.GetAsync(ApartmentRoutes.MyDashboard(query));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.MyDashboard());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnZerosAndExpectedShape_WhenCallerOwnsNoApartments()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var body = await GetDashboardAsync();

        // Assert
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
        body.GetProperty("activeCount").GetInt32().Should().Be(0);
        body.GetProperty("inactiveCount").GetInt32().Should().Be(0);
        body.GetProperty("pendingBookingsCount").GetInt32().Should().Be(0);
        body.GetProperty("currentMonthOccupancyRate").GetDouble().Should().Be(0);
        body.GetProperty("previousMonthOccupancyRate").GetDouble().Should().Be(0);
        body.GetProperty("monthToDateRevenue").GetArrayLength().Should().Be(0);

        var today = DateTime.UtcNow;
        body.GetProperty("currentMonth").GetString().Should().Be($"{today:yyyy-MM}-01");
    }

    [Fact]
    public async Task GetDashboard_ShouldCountActiveAndInactiveApartments()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        var secondId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        var thirdId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        (await HttpClient.PostAsync(ApartmentRoutes.Deactivate(thirdId), null)).EnsureSuccessStatusCode();
        _ = secondId;

        // Act
        var body = await GetDashboardAsync();

        // Assert
        body.GetProperty("totalCount").GetInt32().Should().Be(3);
        body.GetProperty("activeCount").GetInt32().Should().Be(2);
        body.GetProperty("inactiveCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetDashboard_ShouldOnlyIncludeTheCallersApartments()
    {
        // Arrange
        var (tokenA, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(tokenA);
        await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (tokenB, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(tokenB);
        await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        AuthenticateAs(tokenA);

        // Act
        var body = await GetDashboardAsync();

        // Assert
        body.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetDashboard_ShouldNotServeOneOwnersCachedDashboardToAnotherOwner()
    {
        // Arrange — security regression: the cache key contains the owner id, so isolation depends
        // entirely on the endpoint taking it from the token.
        var (tokenA, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(tokenA);
        await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var ownerABody = await GetDashboardAsync(); // populates owner A's cache entry
        ownerABody.GetProperty("totalCount").GetInt32().Should().Be(1);

        var (tokenB, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(tokenB);

        // Act
        var ownerBBody = await GetDashboardAsync();

        // Assert
        ownerBBody.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetDashboard_ShouldIgnoreAnOwnerIdSuppliedInTheQueryString()
    {
        // Arrange
        var (tokenA, _, ownerAId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(tokenA);
        await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (tokenB, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(tokenB);

        // Act
        var body = await GetDashboardAsync($"ownerId={ownerAId}");

        // Assert
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetDashboard_ShouldCountAGuestsReservation_AsPendingForTheOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        AuthenticateAs(ownerToken);

        // Act
        var body = await GetDashboardAsync();

        // Assert
        body.GetProperty("pendingBookingsCount").GetInt32().Should().Be(1);
        body.GetProperty("currentMonthOccupancyRate").GetDouble().Should().Be(0);
        body.GetProperty("monthToDateRevenue").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetDashboard_ShouldReflectChanges_AfterTheCacheIsReset()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var before = await GetDashboardAsync();
        before.GetProperty("totalCount").GetInt32().Should().Be(1);

        await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await Factory.ResetCacheAsync();

        // Act
        var after = await GetDashboardAsync();

        // Assert
        after.GetProperty("totalCount").GetInt32().Should().Be(2);
    }
}