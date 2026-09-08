using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class GetApartmentsByOwnerTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetByOwner_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.ByOwner(ownerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByOwner_ShouldReturnEmptyList_WhenNoApartments()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.ByOwner(ownerId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByOwner_ShouldReturnOnlyThatOwnersActiveApartments_WhenOtherOwnersHaveApartmentsToo()
    {
        // Arrange
        var (tokenA, _, ownerIdA) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(tokenA);
        await HttpClient.PostAsJsonAsync(ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest());

        var (tokenB, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(tokenB);
        await HttpClient.PostAsJsonAsync(ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest());

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.ByOwner(ownerIdA));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results!.Should().ContainSingle();
    }


    [Fact]
    public async Task GetByOwner_ShouldExcludeDeactivatedApartments()
    {
        // Arrange
        var (accessToken, _, ownerId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest());
        var apartmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var deactivateResponse = await HttpClient.PostAsync(ApartmentRoutes.Deactivate(apartmentId), null);
        deactivateResponse.EnsureSuccessStatusCode();

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.ByOwner(ownerId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByOwner_ShouldReturnOnlyActiveOnes_WhenMixOfActiveAndInactive()
    {
        // Arrange
        var (accessToken, _, ownerId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var activeResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest());
        var activeId = await activeResponse.Content.ReadFromJsonAsync<Guid>();

        var inactiveResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest());
        var inactiveId = await inactiveResponse.Content.ReadFromJsonAsync<Guid>();
        await HttpClient.PostAsync(ApartmentRoutes.Deactivate(inactiveId), null);

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.ByOwner(ownerId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results!.Should().ContainSingle();
        results![0].GetProperty("id").GetGuid().Should().Be(activeId);
    }
}