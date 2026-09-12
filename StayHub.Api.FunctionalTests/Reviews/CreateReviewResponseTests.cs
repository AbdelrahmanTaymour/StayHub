using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Reviews;

public sealed class CreateReviewResponseTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, Guid ReviewId)> ArrangeReviewAsync()
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
        var reviewId = await reviewResponse.Content.ReadFromJsonAsync<Guid>();

        return (ownerToken, reviewId);
    }

    [Fact]
    public async Task CreateResponse_ShouldReturnCreatedWithId_WhenCallerIsApartmentOwner()
    {
        // Arrange
        var (ownerToken, reviewId) = await ArrangeReviewAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(reviewId), ReviewTestData.ValidResponseRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateResponse_ShouldReturnCreated_WhenCallerIsAdminNotOwner()
    {
        // Arrange
        var (_, reviewId) = await ArrangeReviewAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(reviewId), ReviewTestData.ValidResponseRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateResponse_ShouldReturnForbidden_WhenCallerIsTheReviewingGuest()
    {
        // Arrange
        var (_, reviewId) = await ArrangeReviewAsync();

        // still authenticated as the guest from ArrangeReviewAsync's last step
        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(reviewId), ReviewTestData.ValidResponseRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ReviewResponse.NotAuthorized");
    }

    [Fact]
    public async Task CreateResponse_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (_, reviewId) = await ArrangeReviewAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(reviewId), ReviewTestData.ValidResponseRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateResponse_ShouldReturnNotFound_WhenReviewDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(Guid.NewGuid()), ReviewTestData.ValidResponseRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateResponse_ShouldReturnConflict_WhenReviewAlreadyHasAResponse()
    {
        // Arrange
        var (ownerToken, reviewId) = await ArrangeReviewAsync();
        AuthenticateAs(ownerToken);
        var firstResponse = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(reviewId), ReviewTestData.ValidResponseRequest());
        firstResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(reviewId), ReviewTestData.ValidResponseRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ReviewResponse.AlreadyRespondedTo");
    }

    [Fact]
    public async Task CreateResponse_ShouldReturnValidationProblem_WhenCommentIsEmpty()
    {
        // Arrange
        var (ownerToken, reviewId) = await ArrangeReviewAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(reviewId), ReviewTestData.ValidResponseRequest(comment: ""));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateResponse_ShouldReturnValidationProblem_WhenCommentExceedsMaxLength()
    {
        // Arrange
        var (ownerToken, reviewId) = await ArrangeReviewAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(reviewId), ReviewTestData.ValidResponseRequest(comment: new string('a', 2001)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateResponse_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ReviewRoutes.Response(reviewId), ReviewTestData.ValidResponseRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}