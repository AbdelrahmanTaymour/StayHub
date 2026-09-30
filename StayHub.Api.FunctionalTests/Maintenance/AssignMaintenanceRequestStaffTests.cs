using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Apartments;

namespace StayHub.Api.FunctionalTests.Maintenance;

public sealed class AssignMaintenanceRequestStaffTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    private async Task<Guid> AssignAsStaffAsync(string ownerToken, Guid apartmentId, Guid staffUserId)
    {
        AuthenticateAs(ownerToken);
        var assignResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.MaintenanceStaff });
        assignResponse.EnsureSuccessStatusCode();
        return staffUserId;
    }

    [Fact]
    public async Task Assign_ShouldReturnNoContent_WhenCallerIsOwnerAndAssigneeIsActiveStaff()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (apartmentId, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        await AssignAsStaffAsync(ownerToken, apartmentId, staffUserId);

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(requestId), new { StaffUserId = staffUserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Assign_ShouldReturnNoContent_WhenCallerIsAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (apartmentId, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        await AssignAsStaffAsync(ownerToken, apartmentId, staffUserId);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(requestId), new { StaffUserId = staffUserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Assign_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (apartmentId, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        await AssignAsStaffAsync(ownerToken, apartmentId, staffUserId);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(requestId), new { StaffUserId = staffUserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.NotAuthorized");
    }

    [Fact]
    public async Task Assign_ShouldReturnForbidden_WhenCallerIsActiveStaffButNotOwnerOrAdmin()
    {
        // Being active staff is enough to be assigned, but not enough to
        // assign someone else — that's an owner/admin action.
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (apartmentId, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (staffToken, _, staffUserId) = await RegisterAndAuthenticateAsync();
        await AssignAsStaffAsync(ownerToken, apartmentId, staffUserId);

        var (_, _, otherStaffUserId) = await RegisterAndAuthenticateAsync();
        await AssignAsStaffAsync(ownerToken, apartmentId, otherStaffUserId);

        AuthenticateAs(staffToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(requestId), new { StaffUserId = otherStaffUserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Assign_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(Guid.NewGuid()), new { StaffUserId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Assign_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(Guid.NewGuid()), new { StaffUserId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Assign_ShouldFail_WhenAssigneeIsNotAnActiveStaffMember()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (_, _, randomUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        // Act — randomUserId was never assigned as staff for this apartment.
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(requestId), new { StaffUserId = randomUserId });

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.AssigneeIsNotActiveStaff");
    }

    [Fact]
    public async Task Assign_ShouldReturnConflict_WhenTicketIsAlreadyClosed()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (apartmentId, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        await AssignAsStaffAsync(ownerToken, apartmentId, staffUserId);

        AuthenticateAs(ownerToken);
        (await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null)).EnsureSuccessStatusCode();
        (await HttpClient.PostAsync(MaintenanceRoutes.Resolve(requestId), null)).EnsureSuccessStatusCode();
        (await HttpClient.PostAsync(MaintenanceRoutes.Close(requestId), null)).EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(requestId), new { StaffUserId = staffUserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.AlreadyClosed");
    }

    [Fact]
    public async Task Assign_ShouldBeReflected_WhenFetchingTheTicketAfterwards()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (apartmentId, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        await AssignAsStaffAsync(ownerToken, apartmentId, staffUserId);

        AuthenticateAs(ownerToken);

        // Act
        var assignResponse = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(requestId), new { StaffUserId = staffUserId });
        assignResponse.EnsureSuccessStatusCode();

        var getResponse = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));
        getResponse.EnsureSuccessStatusCode();

        // Assert
        var item = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        item.GetProperty("assignedToUserId").GetGuid().Should().Be(staffUserId);
    }

    [Fact]
    public async Task Assign_ShouldOverwritePreviousAssignee_WhenReassigned()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (apartmentId, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        var (_, _, firstStaffUserId) = await RegisterAndAuthenticateAsync();
        await AssignAsStaffAsync(ownerToken, apartmentId, firstStaffUserId);

        var (_, _, secondStaffUserId) = await RegisterAndAuthenticateAsync();
        await AssignAsStaffAsync(ownerToken, apartmentId, secondStaffUserId);

        AuthenticateAs(ownerToken);
        (await HttpClient.PostAsJsonAsync(MaintenanceRoutes.Assign(requestId), new { StaffUserId = firstStaffUserId }))
            .EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.Assign(requestId), new { StaffUserId = secondStaffUserId });
        response.EnsureSuccessStatusCode();

        // Assert
        var getResponse = await HttpClient.GetAsync(MaintenanceRoutes.ById(requestId));
        var item = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        item.GetProperty("assignedToUserId").GetGuid().Should().Be(secondStaffUserId);
    }
}