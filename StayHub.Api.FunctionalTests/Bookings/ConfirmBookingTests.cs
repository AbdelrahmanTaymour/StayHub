using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class ConfirmBookingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(Guid ApartmentId, string OwnerToken, Guid BookingId)> ArrangeReservedBookingAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        return (apartmentId, ownerToken, bookingId);
    }

    [Fact]
    public async Task Confirm_ShouldReturnNoContent_WhenCallerIsApartmentOwner()
    {
        // Arrange
        var (_, ownerToken, bookingId) = await ArrangeReservedBookingAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Confirm_ShouldReturnForbidden_WhenCallerIsTheGuestNotTheOwner()
    {
        // The guest who made the booking is NOT the owner, so Confirm should reject them - it's a host action

        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Confirm_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (_, _, bookingId) = await ArrangeReservedBookingAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Confirm_ShouldReturnNoContent_WhenCallerIsAdmin()
    {
        // Arrange
        var (_, _, bookingId) = await ArrangeReservedBookingAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Confirm_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Confirm(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Confirm_ShouldReturnConflict_WhenBookingIsNotInReservedStatus()
    {
        // Arrange
        var (_, ownerToken, bookingId) = await ArrangeReservedBookingAsync();
        AuthenticateAs(ownerToken);
        var firstConfirm = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);
        firstConfirm.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Booking.NotReserved");
    }

    [Fact]
    public async Task Confirm_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}