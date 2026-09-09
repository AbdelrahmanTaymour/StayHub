using System.Net;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class GetBookingsByUserTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetByUser_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ByUser(userId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByUser_ShouldReturnOk_WhenCallerRequestsTheirOwnBookings()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ByUser(userId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByUser_ShouldReturnForbidden_WhenCallerRequestsAnotherUsersBookings()
    {
        // Arrange
        var (_, _, targetUserId) = await RegisterAndAuthenticateAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ByUser(targetUserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Booking.NotAuthorized");
    }

    [Fact]
    public async Task GetByUser_ShouldReturnOk_WhenCallerIsAdmin_ForAnotherUsersBookings()
    {
        // Arrange
        var (_, _, targetUserId) = await RegisterAndAuthenticateAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ByUser(targetUserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}