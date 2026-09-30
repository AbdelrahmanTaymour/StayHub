using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class GetApartmentAvailabilityBlocksTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.AvailabilityBlocks(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Act — anonymous, no token at all
        var response = await HttpClient.GetAsync(ApartmentRoutes.AvailabilityBlocks(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnBlocksWithoutReason_WhenCallerIsAnonymous()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var createBlockResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId),
            ApartmentTestData.BlockRequest());
        createBlockResponse.EnsureSuccessStatusCode();

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.AvailabilityBlocks(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var blocks = body.GetProperty("blocks");
        blocks.GetArrayLength().Should().Be(1);
        blocks[0].GetProperty("reason").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnBlocksWithReason_WhenCallerIsTheOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var createBlockResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId),
            ApartmentTestData.BlockRequest());
        createBlockResponse.EnsureSuccessStatusCode();

        // Act 
        var response = await HttpClient.GetAsync(ApartmentRoutes.AvailabilityBlocks(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var blocks = body.GetProperty("blocks");
        blocks.GetArrayLength().Should().Be(1);
        blocks[0].GetProperty("reason").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnExpectedShape_WhenApartmentHasNoBlocksOrBookings()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.AvailabilityBlocks(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("blocks", out var blocks).Should().BeTrue();
        blocks.GetArrayLength().Should().Be(0);
        body.TryGetProperty("bookedRanges", out var bookedRanges).Should().BeTrue();
        bookedRanges.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnOk_WhenCallerIsAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.AvailabilityBlocks(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnNotFound_WhenApartmentIsInactive_AndCallerIsAnonymous()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        await HttpClient.PostAsync(ApartmentRoutes.Deactivate(apartmentId), null);

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.AvailabilityBlocks(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnOk_WhenApartmentIsInactive_AndCallerIsTheOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        await HttpClient.PostAsync(ApartmentRoutes.Deactivate(apartmentId), null);

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.AvailabilityBlocks(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}