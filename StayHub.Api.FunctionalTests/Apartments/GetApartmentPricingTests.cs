using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Apartments;

public sealed class GetApartmentPricingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetPricing_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest());
        var apartmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Pricing(apartmentId, "start=2026-06-10&end=2026-06-15"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPricing_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Pricing(Guid.NewGuid(), "start=2026-06-10&end=2026-06-15"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPricing_ShouldReturnBadRequest_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest());
        var apartmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Pricing(apartmentId, "start=2026-06-15&end=2026-06-10"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricing_ShouldReturnExpectedShape_WhenApartmentIsAvailable()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var createResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest(priceAmount: 100m));
        var apartmentId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.Pricing(apartmentId, "start=2026-06-10&end=2026-06-15"));

        // Assert
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("isAvailable").GetBoolean().Should().BeTrue();
        body.GetProperty("nights").GetInt32().Should().Be(5);
        body.GetProperty("pricePerNight").GetDecimal().Should().Be(100m);
        body.GetProperty("subtotalForStay").GetDecimal().Should().Be(500m);
        body.TryGetProperty("totalPrice", out _).Should().BeTrue();
        body.TryGetProperty("currency", out _).Should().BeTrue();
    }
}