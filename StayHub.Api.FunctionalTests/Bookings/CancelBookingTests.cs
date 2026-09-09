using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class CancelBookingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, string GuestToken, Guid BookingId)> ArrangeReservedBookingAsync(
        int startOffsetDays = 10)
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);

        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays));

        reserveResponse.EnsureSuccessStatusCode();

        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        return (ownerToken, guestToken, bookingId);
    }

    [Fact]
    public async Task Cancel_ShouldReturnNoContent_WhenCallerIsTheGuest()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeReservedBookingAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Cancel(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancel_ShouldReturnForbidden_WhenCallerIsApartmentOwnerNotTheGuest()
    {
        // Arrange
        var (ownerToken, _, bookingId) = await ArrangeReservedBookingAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Cancel(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (_, _, bookingId) = await ArrangeReservedBookingAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Cancel(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_ShouldReturnNoContent_WhenCallerIsAdmin()
    {
        // Arrange
        var (_, _, bookingId) = await ArrangeReservedBookingAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Cancel(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancel_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Cancel(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_ShouldReturnNoContent_WhenBookingIsConfirmedNotJustReserved()
    {
        // Arrange
        var (ownerToken, guestToken, bookingId) = await ArrangeReservedBookingAsync();
        AuthenticateAs(ownerToken);
        var confirmResponse = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);
        confirmResponse.EnsureSuccessStatusCode();

        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Cancel(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancel_ShouldReturnConflict_WhenBookingIsAlreadyCancelled()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeReservedBookingAsync();
        AuthenticateAs(guestToken);
        var firstCancel = await HttpClient.PostAsync(BookingRoutes.Cancel(bookingId), null);
        firstCancel.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Cancel(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Booking.NotCancellable");
    }

    [Fact]
    public async Task Cancel_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync(BookingRoutes.Cancel(bookingId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}