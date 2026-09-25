using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Application.Apartments.GetApartmentAvailabilityBlocks;
using StayHub.Domain.Apartments;

namespace StayHub.Api.FunctionalTests.Apartments;

public class GetApartmentAvailabilityBlocksTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsCurrentUserAsync(HttpClient);

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.AvailabilityBlocks(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnNotFound_WhenApartmentIdIsMalformed()
    {
        // Act
        var response = await HttpClient.GetAsync("api/v1/apartments/not-a-guid/availability-blocks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnEmptyBlocks_WhenApartmentHasNoAvailabilityBlocks()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsCurrentUserAsync(HttpClient);

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApartmentAvailabilityResponse>();
        result.Should().NotBeNull();
        result!.Blocks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnOnlyRequestedMonth_WhenYearAndMonthAreProvided()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsCurrentUserAsync(HttpClient);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Block 1
        var block1Start = today;
        var block1End = today.AddDays(2);

        // Block 2
        var firstOfNextMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        var block2Start = firstOfNextMonth;
        var block2End = firstOfNextMonth.AddDays(2);

        await ApartmentTestFixtures.AddAvailabilityBlockAsync(
            HttpClient,
            apartmentId,
            start: block1Start,
            end: block1End,
            reason: ApartmentUnavailabilityReason.UnderMaintenance);

        await ApartmentTestFixtures.AddAvailabilityBlockAsync(
            HttpClient,
            apartmentId,
            start: block2Start,
            end: block2End,
            reason: ApartmentUnavailabilityReason.OwnerBlocked);

        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(
            ApartmentRoutes.AvailabilityBlocks(apartmentId, today.Year, today.Month));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApartmentAvailabilityResponse>();
        result.Should().NotBeNull();
        result!.Blocks.Should().ContainSingle();

        var block = result.Blocks.Single();
        block.StartDate.Should().Be(block1Start);
        block.EndDate.Should().Be(block1End);
    }
}