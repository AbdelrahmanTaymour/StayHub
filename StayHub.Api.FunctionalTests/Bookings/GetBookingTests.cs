using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class GetBookingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetBooking_ShouldReturnOk_WhenCallerIsTheGuest()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnOk_WhenCallerIsTheApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnOk_WhenCallerIsAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnNotFound_WhenCallerIsUnrelatedUser()
    {
        // GetBookingQueryHandler intentionally folds "exists but caller isn't guest/owner/admin"
        // into the same 404 as "doesn't exist", to avoid leaking booking existence to callers
        // who have no relationship to it.

        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        var (unrelatedToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(unrelatedToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnExpectedShape_WhenBookingExists()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, guestUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().Be(bookingId);
        body.GetProperty("userId").GetGuid().Should().Be(guestUserId);
        body.GetProperty("apartmentId").GetGuid().Should().Be(apartmentId);
        body.GetProperty("status").GetString().Should().Be("Reserved");
    }
}