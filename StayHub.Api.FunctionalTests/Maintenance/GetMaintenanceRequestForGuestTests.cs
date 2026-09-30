using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Maintenance;

public sealed class GetMaintenanceRequestForGuestTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetForGuest_ShouldReturnOk_WhenCallerIsTheReporter()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();

        var createResponse = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());
        createResponse.EnsureSuccessStatusCode();
        var requestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.Guest(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetForGuest_ShouldReturnForbidden_WhenCallerIsTheOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        var createResponse = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());
        createResponse.EnsureSuccessStatusCode();
        var requestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.Guest(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetForGuest_ShouldReturnForbidden_WhenCallerIsAnUnrelatedGuest()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (reporterToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(reporterToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        var createResponse = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());
        createResponse.EnsureSuccessStatusCode();
        var requestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var (otherGuestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherGuestToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.Guest(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetForGuest_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.Guest(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetForGuest_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.Guest(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetForGuest_ShouldReturnExpectedShape_WithoutReporterContactInfo()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        var createResponse = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest(title: "Leaky tap"));
        createResponse.EnsureSuccessStatusCode();
        var requestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.Guest(requestId));
        response.EnsureSuccessStatusCode();

        // Assert
        var item = await response.Content.ReadFromJsonAsync<JsonElement>();
        item.GetProperty("id").GetGuid().Should().Be(requestId);
        item.GetProperty("title").GetString().Should().Be("Leaky tap");
        item.GetProperty("status").GetString().Should().Be("Open");
        item.TryGetProperty("createdOnUtc", out _).Should().BeTrue();
        item.TryGetProperty("startOnUtc", out _).Should().BeTrue();
        item.TryGetProperty("resolvedOnUtc", out _).Should().BeTrue();
        item.TryGetProperty("closedOnUtc", out _).Should().BeTrue();

        // The confidential/staff-only fields must never appear on this endpoint.
        item.TryGetProperty("reporterEmail", out _).Should().BeFalse();
        item.TryGetProperty("reporterPhoneNumber", out _).Should().BeFalse();
        item.TryGetProperty("reporterFirstName", out _).Should().BeFalse();
        item.TryGetProperty("reporterLastName", out _).Should().BeFalse();
        item.TryGetProperty("reportedByUserId", out _).Should().BeFalse();
        item.TryGetProperty("assignedToUserId", out _).Should().BeFalse();
    }
}