using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class SearchStaffCandidateTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Search_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.StaffSearch(Guid.NewGuid(), "someone@example.com"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.StaffSearch(Guid.NewGuid(), "someone@example.com"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_ShouldReturnNotFound_WhenCallerIsNotTheOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.StaffSearch(apartmentId, "someone@example.com"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_ShouldReturnNotFound_WhenNoAccountMatchesTheEmail()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.StaffSearch(apartmentId, "cleaner.sub@gmail.com"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("User.NotFound");
    }

    [Fact]
    public async Task Search_ShouldReturnIsApartmentOwnerTrue_WhenSearchingTheOwnersOwnEmail()
    {
        // Arrange
        var (ownerToken, ownerRequest, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.StaffSearch(apartmentId, ownerRequest.Email));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("isApartmentOwner").GetBoolean().Should().BeTrue();
        body.GetProperty("isAlreadyAssigned").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Search_ShouldReturnCandidateDetails_WhenCallerIsAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (candidateToken, candidateRequest, _) = await RegisterAndAuthenticateAsync();
        _ = candidateToken;

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.StaffSearch(apartmentId, candidateRequest.Email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}