using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Apartments;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class StaffAssignmentTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    // ---- Assign ----

    [Fact]
    public async Task AssignStaff_ShouldReturnOkWithAssignmentId_WhenCallerIsOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken); // switch back to owner for the assignment call

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.Cleaner });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignmentId = await response.Content.ReadFromJsonAsync<Guid>();
        assignmentId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnForbidden_WhenUserIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.Cleaner });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(Guid.NewGuid()),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.Cleaner });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnNotFound_WhenStaffUserDoesNotExist()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = Guid.NewGuid(), Role = ApartmentStaffRole.Cleaner });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("User");
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnConflict_WhenAlreadyActivelyAssigned()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var first = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.Cleaner });

        first.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.Manager });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ApartmentStaffAssignment.AlreadyAssigned");
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnValidationProblem_WhenInvalidRoleEnum()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = 9999 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- Revoke ----

    [Fact]
    public async Task RevokeStaff_ShouldReturnNoContent_WhenCallerIsOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var assignResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.Cleaner });

        var assignmentId = await assignResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.DeleteAsync(
            ApartmentRoutes.StaffById(assignmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RevokeStaff_ShouldReturnForbidden_WhenUserIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var assignResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.Cleaner });

        var assignmentId = await assignResponse.Content.ReadFromJsonAsync<Guid>();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.DeleteAsync(
            ApartmentRoutes.StaffById(assignmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeStaff_ShouldReturnNotFound_WhenAssignmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.DeleteAsync(
            ApartmentRoutes.StaffById(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RevokeStaff_ShouldReturnConflict_WhenAlreadyRevoked()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (_, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var assignResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = "Cleaner" });

        var assignmentId = await assignResponse.Content.ReadFromJsonAsync<Guid>();

        var firstRevoke = await HttpClient.DeleteAsync(
            ApartmentRoutes.StaffById(assignmentId));

        firstRevoke.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.DeleteAsync(
            ApartmentRoutes.StaffById(assignmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ApartmentStaffAssignment.AlreadyRevoked");
    }
}