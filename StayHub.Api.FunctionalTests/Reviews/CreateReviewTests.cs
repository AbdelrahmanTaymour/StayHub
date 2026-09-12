using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Reviews;

public sealed class CreateReviewTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, string GuestToken, Guid BookingId)> ArrangeCompletedBookingAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);
        var confirmResponse = await HttpClient.PostAsync(BookingRoutes.Confirm(bookingId), null);
        confirmResponse.EnsureSuccessStatusCode();

        await ReviewTestFixtures.CompleteBookingDirectlyAsync(Factory, bookingId);

        return (ownerToken, guestToken, bookingId);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedWithLocationAndId_WhenBookingIsCompletedAndCallerIsTheGuest()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeCompletedBookingAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.BaseRoute, ReviewTestData.ValidCreateRequest(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBe(Guid.Empty);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = ReviewTestData.ValidCreateRequest(Guid.NewGuid());

        // Act
        var response = await HttpClient.PostAsJsonAsync(ReviewRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ReviewTestData.ValidCreateRequest(Guid.NewGuid());

        // Act
        var response = await HttpClient.PostAsJsonAsync(ReviewRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_WhenCallerIsNotTheGuestWhoBooked()
    {
        // Arrange
        var (ownerToken, _, bookingId) = await ArrangeCompletedBookingAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.BaseRoute, ReviewTestData.ValidCreateRequest(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Review.NotAuthorized");
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenBookingIsNotYetCompleted()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();
        // Deliberately left Reserved - not confirmed or completed.

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.BaseRoute, ReviewTestData.ValidCreateRequest(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Review.NotEligible");
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenBookingAlreadyReviewed()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeCompletedBookingAsync();
        AuthenticateAs(guestToken);
        var firstCreate = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.BaseRoute, ReviewTestData.ValidCreateRequest(bookingId));
        firstCreate.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.BaseRoute, ReviewTestData.ValidCreateRequest(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Review.AlreadyReviewed");
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenBookingIdIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ReviewTestData.ValidCreateRequest(Guid.Empty);

        // Act
        var response = await HttpClient.PostAsJsonAsync(ReviewRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task Create_ShouldReturnValidationProblem_WhenRatingIsOutOfRange(int rating)
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeCompletedBookingAsync();
        AuthenticateAs(guestToken);

        var request = ReviewTestData.ValidCreateRequest(bookingId, rating: rating);

        // Act
        var response = await HttpClient.PostAsJsonAsync(ReviewRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenCommentIsEmpty()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeCompletedBookingAsync();
        AuthenticateAs(guestToken);

        var request = ReviewTestData.ValidCreateRequest(bookingId, comment: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(ReviewRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnValidationProblem_WhenCommentExceedsMaxLength()
    {
        // Arrange
        var (_, guestToken, bookingId) = await ArrangeCompletedBookingAsync();
        AuthenticateAs(guestToken);

        var request = ReviewTestData.ValidCreateRequest(bookingId, comment: new string('a', 2001));

        // Act
        var response = await HttpClient.PostAsJsonAsync(ReviewRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}