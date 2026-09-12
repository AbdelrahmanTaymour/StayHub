using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Reviews;

public sealed class GetReviewTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<Guid> ArrangeReviewAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);
        var confirmResponse = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);
        confirmResponse.EnsureSuccessStatusCode();
        await ReviewTestFixtures.CompleteBookingDirectlyAsync(Factory, bookingId);

        AuthenticateAs(guestToken);
        var reviewResponse = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.BaseRoute, ReviewTestData.ValidCreateRequest(bookingId));
        reviewResponse.EnsureSuccessStatusCode();

        return await reviewResponse.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task GetReview_ShouldReturnOk_WhenCallerIsAnonymous()
    {
        // Arrange
        var reviewId = await ArrangeReviewAsync();
        HttpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await HttpClient.GetAsync(ReviewRoutes.ById(reviewId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReview_ShouldReturnNotFound_WhenReviewDoesNotExist()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(ReviewRoutes.ById(reviewId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReview_ShouldReturnExpectedShape_WhenReviewExists()
    {
        // Arrange
        var reviewId = await ArrangeReviewAsync();

        // Act
        var response = await HttpClient.GetAsync(ReviewRoutes.ById(reviewId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().Be(reviewId);
        body.GetProperty("rating").GetInt32().Should().Be(5);
    }
}