using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Apartments;

namespace StayHub.Api.FunctionalTests.Maintenance;

public sealed class GetMaintenanceRequestsByApartmentTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    private async Task<Guid> CreateOpenRequestAsync(Guid apartmentId, string? title = null)
    {
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest(title: title));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnOk_WhenCallerIsOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await CreateOpenRequestAsync(apartmentId);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnOk_WhenCallerIsAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await CreateOpenRequestAsync(apartmentId);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnOk_WhenCallerIsActiveStaff()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await CreateOpenRequestAsync(apartmentId);

        var (staffToken, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var assignResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.MaintenanceStaff });
        assignResponse.EnsureSuccessStatusCode();

        AuthenticateAs(staffToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await CreateOpenRequestAsync(apartmentId);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.NotAuthorized");
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnForbidden_WhenCallerIsAGuestWhoOnlyReportedOneTicket()
    {
        // Distinct from GetMaintenanceRequest (detail): the reporting guest can see their OWN
        // ticket's detail, but NOT the apartment-wide list - confirmed intentional distinction.

        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        await CreateOpenRequestAsync(apartmentId);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnEmptyList_WhenApartmentHasNoRequests()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByApartment_ShouldFilterByStatus_WhenStatusProvided()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var openRequestId = await CreateOpenRequestAsync(apartmentId, title: "Open ticket");
        var inProgressRequestId = await CreateOpenRequestAsync(apartmentId, title: "In-progress ticket");
        var startResponse = await HttpClient.PostAsync(MaintenanceRoutes.Start(inProgressRequestId), null);
        startResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId, "status=Open"));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().ContainSingle();
        results![0].GetProperty("id").GetGuid().Should().Be(openRequestId);
        results[0].GetProperty("status").GetString().Should().Be("Open");
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnAllStatuses_WhenStatusNotProvided()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        await CreateOpenRequestAsync(apartmentId);
        var inProgressRequestId = await CreateOpenRequestAsync(apartmentId);
        var startResponse = await HttpClient.PostAsync(MaintenanceRoutes.Start(inProgressRequestId), null);
        startResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnExpectedShape_WhenRequestsExist()
    {
        // Arrange
        var (ownerToken, _, reporterUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        var requestId = await CreateOpenRequestAsync(apartmentId, title: "Leaky faucet");

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().ContainSingle();
        var item = results![0];
        item.GetProperty("id").GetGuid().Should().Be(requestId);
        item.GetProperty("title").GetString().Should().Be("Leaky faucet");
        item.GetProperty("status").GetString().Should().Be("Open");
        item.GetProperty("reportedByUserId").GetGuid().Should().Be(reporterUserId);
        item.TryGetProperty("description", out _).Should().BeFalse();
    }
}