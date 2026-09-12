using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Maintenance;

public sealed class StartMaintenanceRequestTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Start_ShouldReturnNoContent_WhenCallerIsOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Start_ShouldReturnNoContent_WhenCallerIsAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Start_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.NotAuthorized");
    }

    [Fact]
    public async Task Start_ShouldReturnForbidden_WhenCallerIsGuestWithActiveBookingButNotStaff()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (apartmentId, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Start_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Start(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Start_ShouldReturnConflict_WhenRequestIsNotOpen()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);
        var firstStart = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);
        firstStart.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.NotOpen");
    }

    [Fact]
    public async Task Start_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}