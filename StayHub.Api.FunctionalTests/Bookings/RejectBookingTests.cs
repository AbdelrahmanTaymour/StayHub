using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class RejectBookingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, Guid BookingId)> ArrangeReservedBookingAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        return (ownerToken, bookingId);
    }

    [Fact]
    public async Task Reject_ShouldReturnNoContent_WhenCallerIsApartmentOwner()
    {
        // Arrange
        var (ownerToken, bookingId) = await ArrangeReservedBookingAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Reject(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Reject_ShouldReturnForbidden_WhenCallerIsTheGuestNotTheOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Reject(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reject_ShouldReturnNoContent_WhenCallerIsAdmin()
    {
        // Arrange
        var (_, bookingId) = await ArrangeReservedBookingAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Reject(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Reject_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Reject(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reject_ShouldReturnConflict_WhenBookingIsNotInReservedStatus()
    {
        // Arrange
        var (ownerToken, bookingId) = await ArrangeReservedBookingAsync();
        AuthenticateAs(ownerToken);
        var firstReject = await HttpClient.PostAsync(BookingRoutes.Reject(bookingId), null);
        firstReject.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Reject(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Booking.NotReserved");
    }

    [Fact]
    public async Task Reject_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Reject(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}