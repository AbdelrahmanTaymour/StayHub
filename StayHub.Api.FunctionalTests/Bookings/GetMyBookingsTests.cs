using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class GetMyBookingsTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetMine_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Mine());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMine_ShouldReturnEmptyList_WhenCallerHasNoBookings()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Mine());
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMine_ShouldReturnOnlyCallersOwnBookings()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestAToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestAToken);
        var reserveA = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 10));
        reserveA.EnsureSuccessStatusCode();

        var (guestBToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestBToken);
        var reserveB = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 30));
        reserveB.EnsureSuccessStatusCode();

        AuthenticateAs(guestAToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Mine());
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().ContainSingle();
    }
}