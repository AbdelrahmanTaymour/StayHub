using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Conversations;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class GetBookingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetBooking_ShouldReturnOk_WhenCallerIsTheGuest()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnOk_WhenCallerIsTheApartmentOwner()
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
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnOk_WhenCallerIsAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnNotFound_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        var (unrelatedToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(unrelatedToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnExpectedShape_WhenBookingExists()
    {
        // Arrange
        var (ownerToken, _, ownerUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().Be(bookingId);
        body.GetProperty("apartmentId").GetGuid().Should().Be(apartmentId);
        body.GetProperty("status").GetString().Should().Be("Reserved");

        body.GetProperty("apartmentName").GetString().Should().NotBeNullOrWhiteSpace();
        body.TryGetProperty("apartmentImageUrl", out _).Should().BeTrue();

        body.TryGetProperty("address", out var address).Should().BeTrue();
        address.GetProperty("city").GetString().Should().NotBeNullOrWhiteSpace();
        address.GetProperty("street").GetString().Should().NotBeNullOrWhiteSpace();

        body.GetProperty("nights").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("currency").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("pricePerNight").GetDecimal().Should().BeGreaterThan(0);
        body.TryGetProperty("priceForPeriodAmount", out _).Should().BeTrue();
        body.TryGetProperty("cleaningFeeAmount", out _).Should().BeTrue();
        body.TryGetProperty("amenitiesUpChargeAmount", out _).Should().BeTrue();
        body.TryGetProperty("totalPriceAmount", out _).Should().BeTrue();

        body.TryGetProperty("createdOnUtc", out _).Should().BeTrue();
        body.TryGetProperty("updatedOnUtc", out _).Should().BeTrue();

        body.TryGetProperty("host", out var host).Should().BeTrue();
        host.GetProperty("id").GetGuid().Should().Be(ownerUserId);
        host.GetProperty("fullName").GetString().Should().NotBeNullOrWhiteSpace();

        body.GetProperty("conversationId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetBooking_ShouldNotExposeRemovedLegacyFields()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("userId", out _).Should().BeFalse();
        body.TryGetProperty("priceAmount", out _).Should().BeFalse();
        body.TryGetProperty("priceCurrency", out _).Should().BeFalse();
        body.TryGetProperty("cleaningFeeCurrency", out _).Should().BeFalse();
        body.TryGetProperty("amenitiesUpChargeCurrency", out _).Should().BeFalse();
        body.TryGetProperty("totalPriceCurrency", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnCanCancelTrue_WhenCallerIsTheGuest()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("canCancel").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnCanCancelFalse_WhenCallerIsTheApartmentOwner()
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
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("canCancel").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnConversationId_WhenGuestHasStartedAConversationWithTheHost()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var bookingId = await BookingTestFixtures.ReserveAsync(HttpClient, apartmentId);

        var startResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(apartmentId));
        startResponse.EnsureSuccessStatusCode();
        var conversationId = await startResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ById(bookingId));
        response.EnsureSuccessStatusCode();

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("conversationId").GetGuid().Should().Be(conversationId);
    }
}