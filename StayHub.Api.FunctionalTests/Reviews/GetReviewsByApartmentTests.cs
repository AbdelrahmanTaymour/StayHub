using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Reviews;

public sealed class GetReviewsByApartmentTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetByApartment_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(ReviewRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnEmptyList_WhenApartmentHasNoReviews()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(ReviewRoutes.ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnOnlyThatApartmentsReviews()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);
        var confirmResponse = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);
        confirmResponse.EnsureSuccessStatusCode();
        await ReviewTestData.CompleteBookingDirectlyAsync(Factory, bookingId);

        AuthenticateAs(guestToken);
        var reviewResponse = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.BaseRoute, ReviewTestData.ValidCreateRequest(bookingId));
        reviewResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(ReviewRoutes.ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().ContainSingle();
    }
}