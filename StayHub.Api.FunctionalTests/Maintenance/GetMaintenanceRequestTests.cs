using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Apartments;

namespace StayHub.Api.FunctionalTests.Maintenance;

public sealed class GetMaintenanceRequestTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetById_ShouldReturnOk_WhenCallerIsOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenCallerIsAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenCallerIsActiveStaff()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (apartmentId, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (staffToken, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var assignResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.MaintenanceStaff });
        assignResponse.EnsureSuccessStatusCode();

        AuthenticateAs(staffToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.NotAuthorized");
    }

    [Fact]
    public async Task GetById_ShouldReturnForbidden_WhenCallerIsTheReportingGuestButNotStaff()
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
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_ShouldReturnExpectedShape_IncludingReporterContactInfoAndAssignedToUserId()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, reporterUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        var createResponse = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest(title: "Broken AC"));
        createResponse.EnsureSuccessStatusCode();
        var requestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));
        response.EnsureSuccessStatusCode();

        // Assert
        var item = await response.Content.ReadFromJsonAsync<JsonElement>();
        item.GetProperty("id").GetGuid().Should().Be(requestId);
        item.GetProperty("title").GetString().Should().Be("Broken AC");
        item.GetProperty("status").GetString().Should().Be("Open");
        item.GetProperty("reportedByUserId").GetGuid().Should().Be(reporterUserId);
        item.TryGetProperty("reporterFirstName", out _).Should().BeTrue();
        item.TryGetProperty("reporterLastName", out _).Should().BeTrue();
        item.TryGetProperty("reporterEmail", out _).Should().BeTrue();
        item.TryGetProperty("startOnUtc", out var startOnUtc).Should().BeTrue();
        startOnUtc.ValueKind.Should().Be(JsonValueKind.Null);
        item.TryGetProperty("assignedToUserId", out var assignedToUserId).Should().BeTrue();
        assignedToUserId.ValueKind.Should().Be(JsonValueKind.Null);
        item.TryGetProperty("description", out _).Should().BeTrue();
    }
}