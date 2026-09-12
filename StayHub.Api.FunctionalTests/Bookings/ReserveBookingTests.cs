using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class ReserveBookingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Reserve_ShouldReturnCreatedWithLocationAndId_WhenRequestIsValid()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBe(Guid.Empty);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Reserve_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = BookingTestData.ValidReserveRequest(Guid.NewGuid());

        // Act
        var response = await HttpClient.PostAsJsonAsync(BookingRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reserve_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = BookingTestData.ValidReserveRequest(Guid.NewGuid());

        // Act
        var response = await HttpClient.PostAsJsonAsync(BookingRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reserve_ShouldReturnValidationProblem_WhenApartmentIdIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = BookingTestData.ValidReserveRequest(Guid.Empty);

        // Act
        var response = await HttpClient.PostAsJsonAsync(BookingRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reserve_ShouldReturnValidationProblem_WhenStartDateIsNotBeforeEndDate()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);

        var sameDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var request = new { ApartmentId = apartmentId, StartDate = sameDate, EndDate = sameDate };

        // Act
        var response = await HttpClient.PostAsJsonAsync(BookingRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reserve_ShouldReturnValidationProblem_WhenEndDateBeforeStartDate()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var end = start.AddDays(-2);
        var request = new { ApartmentId = apartmentId, StartDate = start, EndDate = end };

        // Act
        var response = await HttpClient.PostAsJsonAsync(BookingRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reserve_ShouldReturnConflict_WhenOverlappingAnExistingBooking()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestAToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestAToken);
        var firstReserve = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute,
            BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 10, durationDays: 5));
        firstReserve.EnsureSuccessStatusCode();

        var (guestBToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestBToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute,
            BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 12, durationDays: 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Booking.Overlap");
    }

    [Fact]
    public async Task Reserve_ShouldReturnCreated_WhenNonOverlappingWithExistingBooking()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestAToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestAToken);
        var firstReserve = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute,
            BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 10, durationDays: 5));
        firstReserve.EnsureSuccessStatusCode();

        var (guestBToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestBToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute,
            BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 20, durationDays: 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Reserve_ShouldSucceed_WhenOwnerBooksTheirOwnApartment()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}