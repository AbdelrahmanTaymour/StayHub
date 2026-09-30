using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Conversations;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Bookings;

public sealed class GetConversationBookingDetailsTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetByConversation_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ByConversation(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnNotFound_WhenConversationDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ByConversation(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnNotFound_WhenConversationHasNoLinkedBooking()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var startResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(apartmentId));
        startResponse.EnsureSuccessStatusCode();
        var conversationId = await startResponse.Content.ReadFromJsonAsync<Guid>();

        // Act — inquiry-only conversation, never linked to a booking.
        var response = await HttpClient.GetAsync(BookingRoutes.ByConversation(conversationId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnNotFound_WhenCallerIsNotAParticipant()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 10));
        reserveResponse.EnsureSuccessStatusCode();
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        var startResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(apartmentId, bookingId));
        startResponse.EnsureSuccessStatusCode();
        var conversationId = await startResponse.Content.ReadFromJsonAsync<Guid>();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ByConversation(conversationId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnExpectedShape_WhenCallerIsTheGuest()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 10));
        reserveResponse.EnsureSuccessStatusCode();
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        var startResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(apartmentId, bookingId));
        startResponse.EnsureSuccessStatusCode();
        var conversationId = await startResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ByConversation(conversationId));

        // Assert
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("bookingId").GetGuid().Should().Be(bookingId);
        body.GetProperty("apartmentId").GetGuid().Should().Be(apartmentId);
        body.TryGetProperty("checkIn", out _).Should().BeTrue();
        body.TryGetProperty("checkOut", out _).Should().BeTrue();
        body.TryGetProperty("nights", out _).Should().BeTrue();
        body.TryGetProperty("hostName", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnOk_WhenCallerIsTheOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays: 10));
        reserveResponse.EnsureSuccessStatusCode();
        var bookingId = await reserveResponse.Content.ReadFromJsonAsync<Guid>();

        var startResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(apartmentId, bookingId));
        startResponse.EnsureSuccessStatusCode();
        var conversationId = await startResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(BookingRoutes.ByConversation(conversationId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}