using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Reviews;

public sealed class GetApartmentReviewsTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private static string ByApartment(Guid apartmentId, string? query = null) =>
        $"api/v1/reviews/by-apartment/{apartmentId}" + (query is null ? "" : $"?{query}");

    [Fact]
    public async Task GetByApartment_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnEmptyPagedEnvelope_WhenNoReviewsExist()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().Be(0);
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnPagedEnvelope_WithExpectedShape()
    {
        // Act
        var response = await HttpClient.GetAsync(ByApartment(Guid.NewGuid(), "page=1&pageSize=5"));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("items", out _).Should().BeTrue();
        body.TryGetProperty("page", out _).Should().BeTrue();
        body.TryGetProperty("pageSize", out _).Should().BeTrue();
        body.TryGetProperty("totalCount", out _).Should().BeTrue();
        body.TryGetProperty("totalPages", out _).Should().BeTrue();
    }
}