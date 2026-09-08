using System.Net;
using System.Net.Http.Json;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class AvailabilityBlockTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    // ---- Create ----

    [Fact]
    public async Task CreateBlock_ShouldReturnOkWithId_WhenCallerIsApartmentOwner()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId), ApartmentTestData.BlockRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var blockId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, blockId);
    }

    [Fact]
    public async Task CreateBlock_ShouldReturnForbidden_WhenCallerIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId), ApartmentTestData.BlockRequest());

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateBlock_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(Guid.NewGuid()), ApartmentTestData.BlockRequest());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBlock_ShouldReturnBadRequest_WhenStartDateIsInThePast()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId), ApartmentTestData.BlockRequest(-5));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBlock_ShouldReturnBadRequest_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        var end = start.AddDays(-2);

        // Act
        var response = await HttpClient.PostAsJsonAsync(ApartmentRoutes.AvailabilityBlocks(apartmentId),
            new
            {
                Start = start,
                End = end,
                Reason = "OwnerBlocked"
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBlock_ShouldReturnBadRequest_WhenReasonValueIsInvalid()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId),
            new
            {
                Start = start,
                End = start.AddDays(1),
                Reason = 9999
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBlock_ShouldReturnConflict_WhenRangeOverlapsExistingBlock()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var first = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId), ApartmentTestData.BlockRequest(10, 5));

        first.EnsureSuccessStatusCode();

        // Act
        // Overlaps the first block's [10, 15) range.
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId), ApartmentTestData.BlockRequest(12, 3));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Overlap", body);
    }

    [Fact]
    public async Task CreateBlock_ShouldReturnOk_WhenRangeDoesNotOverlapExistingBlock()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var first = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId), ApartmentTestData.BlockRequest(10, 5));

        first.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId),
            ApartmentTestData.BlockRequest(20, 3));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---- Remove ----

    [Fact]
    public async Task RemoveBlock_ShouldReturnNoContent_WhenCallerIsApartmentOwner()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId),
            ApartmentTestData.BlockRequest());

        var blockId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.DeleteAsync(ApartmentRoutes.AvailabilityBlockById(blockId));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemoveBlock_ShouldReturnForbidden_WhenCallerIsNotApartmentOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);

        var apartmentId = await ApartmentTestData.CreateApartmentAsOwnerAsync(HttpClient);

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId), ApartmentTestData.BlockRequest());

        var blockId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.DeleteAsync(
            ApartmentRoutes.AvailabilityBlockById(blockId));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveBlock_ShouldReturnNotFound_WhenBlockDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.DeleteAsync(
            ApartmentRoutes.AvailabilityBlockById(Guid.NewGuid()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}