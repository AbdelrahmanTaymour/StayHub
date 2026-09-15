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
    private async Task<(string OwnerToken, Guid ApartmentId, Guid RequestId)> ArrangeOpenRequestAsOwnerAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var createResponse = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());
        createResponse.EnsureSuccessStatusCode();
        var requestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        return (ownerToken, apartmentId, requestId);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldReturnOk_WhenCallerIsOwner()
    {
        // Arrange
        var (ownerToken, _, requestId) = await ArrangeOpenRequestAsOwnerAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldReturnOk_WhenCallerIsAdmin()
    {
        // Arrange
        var (_, _, requestId) = await ArrangeOpenRequestAsOwnerAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldReturnOk_WhenCallerIsActiveStaff()
    {
        // Arrange
        var (ownerToken, apartmentId, requestId) = await ArrangeOpenRequestAsOwnerAsync();

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
    public async Task GetMaintenanceRequest_ShouldReturnOk_WhenCallerIsTheReportingGuest()
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (_, _, requestId) = await ArrangeOpenRequestAsOwnerAsync();

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
    public async Task GetMaintenanceRequest_ShouldReturnNotFound_WhenRequestDoesNotExist()
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
    public async Task GetMaintenanceRequest_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldReturnExpectedShape_WhenRequestExists()
    {
        // Arrange
        var (ownerToken, apartmentId, requestId) = await ArrangeOpenRequestAsOwnerAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().Be(requestId);
        body.GetProperty("apartmentId").GetGuid().Should().Be(apartmentId);
        body.GetProperty("status").GetString().Should().Be("Open");
        body.TryGetProperty("description", out var description).Should().BeTrue();
        description.GetString().Should().NotBeNullOrWhiteSpace();
        body.TryGetProperty("reporterEmail", out var reporterEmail).Should().BeTrue();
        reporterEmail.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldReflectStatusTransitions()
    {
        // Arrange
        var (ownerToken, _, requestId) = await ArrangeOpenRequestAsOwnerAsync();
        AuthenticateAs(ownerToken);
        var startResponse = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);
        startResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("InProgress");
    }
}