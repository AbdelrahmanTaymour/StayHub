using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class GetApartmentTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetApartment_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var createResponse = await HttpClient.PostAsJsonAsync(ApartmentRoutes.BaseRoute,
            ApartmentTestData.ValidCreateRequest());

        var apartmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.ById(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApartment_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Act
        var response = await HttpClient.GetAsync(ApartmentRoutes.ById(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetApartment_ShouldReturnNotFound_WhenApartmentIdIsMalformed()
    {
        // Act
        var response = await HttpClient.GetAsync("api/v1/apartments/not-a-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetApartment_ShouldReturnExpectedResponse_WhenApartmentExists()
    {
        // Arrange
        var (accessToken, _, userId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ApartmentTestData.ValidCreateRequest();

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Search(),
            request);

        var apartmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.ById(apartmentId));

        // Assert
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("id").GetGuid().Should().Be(apartmentId);
        body.GetProperty("ownerId").GetGuid().Should().Be(userId);
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();

        body.TryGetProperty("address", out var address).Should().BeTrue();
        address.GetProperty("city").GetString().Should().NotBeNullOrWhiteSpace();

        body.TryGetProperty("images", out var images).Should().BeTrue();
        images.GetArrayLength().Should().Be(0);
    }
}