using System.Net;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class ActivateDeactivateApartmentTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    // ---- Deactivate ----

    [Fact]
    public async Task Deactivate_ShouldReturnNoContent_WhenAsOwner_OnActiveApartment()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Deactivate(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Deactivate_ShouldReturnForbidden_WhenUserIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Deactivate(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivate_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Deactivate(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_ShouldReturnConflict_WhenAlreadyInactiveApartment()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var firstDeactivate = await HttpClient.PostAsync(
            ApartmentRoutes.Deactivate(apartmentId), null);

        firstDeactivate.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Deactivate(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Apartment.AlreadyInactive");
    }

    [Fact]
    public async Task Deactivate_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Deactivate(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Activate ----

    [Fact]
    public async Task Activate_ShouldReturnNoContent_WhenAsOwner_OnInactiveApartment()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        await HttpClient.PostAsync(ApartmentRoutes.Deactivate(apartmentId), null);

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Activate(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Activate_ShouldReturnForbidden_WhenUserIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        await HttpClient.PostAsync(ApartmentRoutes.Deactivate(apartmentId), null);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Activate(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Activate_ShouldReturnConflict_WhenAlreadyActiveApartment()
    {
        // Arrange
        // Newly created apartments are active by default (Apartment.Create sets isActive: true).
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Activate(apartmentId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Apartment.AlreadyActive");
    }

    [Fact]
    public async Task Activate_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(ApartmentRoutes.Activate(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}