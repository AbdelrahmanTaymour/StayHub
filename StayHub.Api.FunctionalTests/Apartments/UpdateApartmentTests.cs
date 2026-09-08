using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class UpdateApartmentTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenCallerIsOwner()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(apartmentId),
            ApartmentTestData.ValidUpdateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_ShouldPersistChanges_WhenCacheIsReset()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);
        var newName = $"Renamed {Guid.NewGuid():N}";

        // Act
        var updateResponse = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(apartmentId),
            ApartmentTestData.ValidUpdateRequest(name: newName));

        updateResponse.EnsureSuccessStatusCode();

        // Reset the cache to ensure the GET verifies the persisted value rather than a stale entry.
        await Factory.ResetCacheAsync();

        var getResponse = await HttpClient.GetAsync(
            ApartmentRoutes.ById(apartmentId));

        getResponse.EnsureSuccessStatusCode();

        // Assert
        var body = await getResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        body!.GetProperty("name").GetString().Should().Be(newName);
    }

    [Fact]
    public async Task Update_ShouldReturnUnauthorized_WhenCallerIsNotAuthenticated()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(apartmentId),
            ApartmentTestData.ValidUpdateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_ShouldReturnForbidden_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(apartmentId),
            ApartmentTestData.ValidUpdateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Apartment.NotAuthorized");
    }

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenCallerIsAdminButNotOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);

        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(apartmentId),
            ApartmentTestData.ValidUpdateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(Guid.NewGuid()),
            ApartmentTestData.ValidUpdateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenApartmentIdIsMalformed()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            "api/v1/apartments/not-a-guid",
            ApartmentTestData.ValidUpdateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturnValidationProblem_WhenNameIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(apartmentId),
            ApartmentTestData.ValidUpdateRequest(name: ""));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnValidationProblem_WhenPriceAmountIsZero()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(apartmentId),
            ApartmentTestData.ValidUpdateRequest(priceAmount: 0));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnValidationProblem_WhenPriceCurrencyIsUnsupported()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(apartmentId),
            ApartmentTestData.ValidUpdateRequest(priceCurrency: "ZZZ"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnValidationProblem_WhenCleaningFeeIsNegative()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PutAsJsonAsync(
            ApartmentRoutes.ById(apartmentId),
            ApartmentTestData.ValidUpdateRequest(cleaningFeeAmount: -1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}