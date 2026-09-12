using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Payments;

public sealed class GetPaymentByBookingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, string GuestToken, Guid BookingId)> ArrangeInitiatedPaymentAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);
        var confirmResponse = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);
        confirmResponse.EnsureSuccessStatusCode();

        AuthenticateAs(guestToken);
        var initiateResponse = await HttpClient.PostAsJsonAsync(PaymentRoutes.BaseRoute, new { BookingId = bookingId });
        initiateResponse.EnsureSuccessStatusCode();

        return (ownerToken, guestToken, bookingId);
    }

    [Fact]
    public async Task GetByBooking_ShouldReturnOk_WhenCallerIsTheGuest()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeInitiatedPaymentAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.GetAsync(PaymentRoutes.ByBooking(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByBooking_ShouldReturnOk_WhenCallerIsTheApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, bookingId) = await ArrangeInitiatedPaymentAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(PaymentRoutes.ByBooking(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByBooking_ShouldReturnOk_WhenCallerIsAdmin()
    {
        // Arrange
        var (_, _, bookingId) = await ArrangeInitiatedPaymentAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(PaymentRoutes.ByBooking(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByBooking_ShouldReturnNotFound_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (_, _, bookingId) = await ArrangeInitiatedPaymentAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.GetAsync(PaymentRoutes.ByBooking(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByBooking_ShouldReturnNotFound_WhenNoPaymentExistsForBooking()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();
        // No payment initiated for this booking.

        // Act
        var response = await HttpClient.GetAsync(PaymentRoutes.ByBooking(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByBooking_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(PaymentRoutes.ByBooking(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByBooking_ShouldReturnExpectedShape_WhenPaymentExists()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeInitiatedPaymentAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.GetAsync(PaymentRoutes.ByBooking(bookingId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("bookingId").GetGuid().Should().Be(bookingId);
        body.GetProperty("status").GetString().Should().Be("Pending");
    }
}