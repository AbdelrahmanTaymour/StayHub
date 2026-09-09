using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Maintenance;

public sealed class CreateMaintenanceRequestTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Create_ShouldReturnCreatedWithLocationAndId_WhenCallerIsOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBe(Guid.Empty);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenCallerIsAdminNotOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenCallerIsGuestWithActiveBooking()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_WhenCallerHasNoBookingAndIsNotOwnerOrAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (unrelatedToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(unrelatedToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.NotAuthorized");
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_WhenGuestBookingIsNotActive()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();
        var cancelResponse = await HttpClient.PostAsync(BookingRoutes.Cancel(bookingId), null);
        cancelResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(Guid.NewGuid()), MaintenanceTestData.ValidCreateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = MaintenanceTestData.ValidCreateRequest();

        // Act
        var response = await HttpClient.PostAsJsonAsync(MaintenanceRoutes.CreateRequest(Guid.NewGuid()), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenTitleIsEmpty()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var request = MaintenanceTestData.ValidCreateRequest(title: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(MaintenanceRoutes.CreateRequest(apartmentId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var request = MaintenanceTestData.ValidCreateRequest(title: new string('a', 201));

        // Act
        var response = await HttpClient.PostAsJsonAsync(MaintenanceRoutes.CreateRequest(apartmentId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenDescriptionIsEmpty()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var request = MaintenanceTestData.ValidCreateRequest(description: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(MaintenanceRoutes.CreateRequest(apartmentId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var request = MaintenanceTestData.ValidCreateRequest(description: new string('a', 2001));

        // Act
        var response = await HttpClient.PostAsJsonAsync(MaintenanceRoutes.CreateRequest(apartmentId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}