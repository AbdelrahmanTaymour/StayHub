using System.Net;
using System.Net.Http.Json;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Apartments;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class ApartmentAmenityTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    // ---- Add ----

    [Fact]
    public async Task AddAmenity_ShouldReturnNoContent_WhenCallerIsApartmentOwner()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Amenities(apartmentId),
            new { Amenity = Amenity.WiFi });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddAmenity_ShouldReturnForbidden_WhenCallerIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Amenities(apartmentId),
            new { Amenity = Amenity.WiFi });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddAmenity_ShouldReturnConflict_WhenAmenityIsAlreadyAdded()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var first = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Amenities(apartmentId),
            new { Amenity = Amenity.WiFi });

        first.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Amenities(apartmentId),
            new { Amenity = Amenity.WiFi });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Apartment.AmenityAlreadyAdded", body);
    }

    [Fact]
    public async Task AddAmenity_ShouldReturnBadRequest_WhenAmenityValueIsInvalid()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Amenities(apartmentId),
            new { Amenity = 9999 });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddAmenity_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Amenities(Guid.NewGuid()),
            new { Amenity = Amenity.WiFi });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Remove ----

    [Fact]
    public async Task RemoveAmenity_ShouldReturnNoContent_WhenCallerIsApartmentOwner()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Amenities(apartmentId),
            new { Amenity = Amenity.WiFi });

        // Act
        var response = await HttpClient.DeleteAsync(
            $"{ApartmentRoutes.Amenities(apartmentId)}?amenity=WiFi");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemoveAmenity_ShouldReturnForbidden_WhenCallerIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Amenities(apartmentId),
            new { Amenity = Amenity.WiFi });

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.DeleteAsync(
            $"{ApartmentRoutes.Amenities(apartmentId)}?amenity=WiFi");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveAmenity_ShouldReturnNotFound_WhenAmenityIsNotAssignedToApartment()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.DeleteAsync(
            $"{ApartmentRoutes.Amenities(apartmentId)}?amenity=WiFi");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Apartment.AmenityNotFound", body);
    }

    [Fact]
    public async Task RemoveAmenity_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.DeleteAsync(
            $"{ApartmentRoutes.Amenities(Guid.NewGuid())}?amenity=WiFi");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}