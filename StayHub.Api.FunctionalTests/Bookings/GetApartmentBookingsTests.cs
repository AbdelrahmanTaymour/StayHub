using System.Net;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class GetApartmentBookingsTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetApartmentBookings_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Bookings(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Bookings(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnOk_WhenCallerIsTheOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Bookings(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnForbidden_WhenCallerIsNotTheOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Bookings(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Apartment.NotAuthorized");
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnOk_WhenCallerIsAdmin_ForAnotherOwnersApartment()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Bookings(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnBooking_WhenApartmentHasABooking()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.Bookings(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(bookingId.ToString());
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldFilterByGuestName_WhenSearchIsProvided()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (matchingGuestToken, matchingGuestRequest, _) =
            await RegisterAndAuthenticateAsync(firstName: "Matching", lastName: "Guest");
        AuthenticateAs(matchingGuestToken);
        var matchingBookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId, startOffsetDays: 10);

        var (otherGuestToken, _, _) = await RegisterAndAuthenticateAsync(firstName: "Other", lastName: "Guest");
        AuthenticateAs(otherGuestToken);
        var otherBookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId, startOffsetDays: 40);

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(
            BookingRoutes.Bookings(apartmentId, $"search={Uri.EscapeDataString(matchingGuestRequest.FirstName)}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(matchingBookingId.ToString());
        body.Should().NotContain(otherBookingId.ToString());
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnNoBookings_WhenStatusFilterDoesNotMatch()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        AuthenticateAs(ownerToken);

        // Act (the booking is only Reserved, so Cancelled should exclude it)
        var response = await HttpClient.GetAsync(BookingRoutes.Bookings(apartmentId, "status=Cancelled"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(bookingId.ToString());
    }
}