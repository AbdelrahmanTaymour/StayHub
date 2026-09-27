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
        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Mine());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMine_ShouldReturnEmptyPagedEnvelope_WhenCallerHasNoBookings()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Mine());
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMine_ShouldReturnOnlyCallersOwnBookings()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

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
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetMine_ShouldReturnApartmentAndCancelDetails_WithExpectedPropertyNames()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserve = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 30));
        reserve.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Mine());
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();

        var item = result.Items[0];
        item.TryGetProperty("apartmentName", out _).Should().BeTrue();
        item.TryGetProperty("apartmentCity", out _).Should().BeTrue();
        item.TryGetProperty("pricePerNight", out _).Should().BeTrue();
        item.TryGetProperty("nights", out _).Should().BeTrue();
        item.TryGetProperty("canCancel", out _).Should().BeTrue();
        item.GetProperty("canCancel").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetMine_ShouldFilterByUpcoming_WhenFilterQueryParamIsProvided()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserve = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 30));
        reserve.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Mine("filter=Upcoming"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetMine_ShouldReturnEmpty_WhenFilterIsCompleted_AndNoBookingsAreCompleted()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserve = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 30));
        reserve.EnsureSuccessStatusCode();

        // Act — the freshly reserved booking is upcoming, not completed.
        var response = await HttpClient.GetAsync(BookingRoutes.Mine("filter=Completed"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
    }

    private sealed record PagedResponseDto<T>(
        List<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}