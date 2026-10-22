using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Conversations;

public sealed class StartConversationTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Start_ShouldReturnCreatedWithLocationAndId_WhenRequestIsValid()
    {
        // Arrange
        var (ownerToken, _, ownerId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(ownerId, apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBe(Guid.Empty);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Start_ShouldReturnCreated_WhenNoApartmentOrBookingIsGiven()
    {
        // Arrange — the core new capability: messaging someone directly
        // (e.g. from an owner profile page), nothing else attached.
        var (_, _, recipientId) = await RegisterAndAuthenticateAsync();

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(recipientId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Start_ShouldReturnSameConversationId_WhenMessagingSameRecipientAboutADifferentApartment()
    {
        // Arrange — identity is the participant pair alone now: messaging
        // the same owner about a second listing reuses the same thread.
        var (ownerToken, _, ownerId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var firstApartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        var secondApartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var firstResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(ownerId, firstApartmentId));
        firstResponse.EnsureSuccessStatusCode();
        var firstConversationId = await firstResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var secondResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(ownerId, secondApartmentId));

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondConversationId = await secondResponse.Content.ReadFromJsonAsync<Guid>();
        secondConversationId.Should().Be(firstConversationId);
    }

    [Fact]
    public async Task Start_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = ConversationTestData.ValidStartRequest(Guid.NewGuid());

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Start_ShouldReturnNotFound_WhenRecipientDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ConversationTestData.ValidStartRequest(Guid.NewGuid());

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("RecipientNotFound");
    }

    [Fact]
    public async Task Start_ShouldReturnNotFound_WhenApartmentIdGivenButDoesNotExist()
    {
        // Arrange — a valid recipient, but an apartment id that doesn't exist.
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        var (_, _, recipientId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ConversationTestData.ValidStartRequest(recipientId, Guid.NewGuid());

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Start_ShouldReturnValidationProblem_WhenCallerMessagesThemself()
    {
        // Arrange
        var (accessToken, _, selfId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(selfId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("CannotMessageSelf");
    }

    [Fact]
    public async Task Start_ShouldReturnValidationProblem_WhenRecipientIdIsEmpty()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var request = ConversationTestData.ValidStartRequest(Guid.Empty);

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_ShouldReturnValidationProblem_WhenInitialMessageIsEmpty()
    {
        // Arrange
        var (_, _, recipientId) = await RegisterAndAuthenticateAsync();
        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);

        var request = ConversationTestData.ValidStartRequest(recipientId, initialMessage: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_ShouldReturnValidationProblem_WhenInitialMessageExceedsMaxLength()
    {
        // Arrange
        var (_, _, recipientId) = await RegisterAndAuthenticateAsync();
        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);

        var request = ConversationTestData.ValidStartRequest(recipientId, initialMessage: new string('a', 4001));

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_ShouldReturnCreated_WhenInitialMessageIsExactlyMaxLength()
    {
        // Arrange
        var (_, _, recipientId) = await RegisterAndAuthenticateAsync();
        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);

        var request = ConversationTestData.ValidStartRequest(recipientId, initialMessage: new string('a', 4000));

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Start_ShouldReturnSameConversationId_WhenCalledTwiceBetweenSameParticipants()
    {
        // Arrange
        var (ownerToken, _, ownerId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var firstResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(ownerId, apartmentId));
        firstResponse.EnsureSuccessStatusCode();
        var firstConversationId = await firstResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var secondResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(ownerId, apartmentId));

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondConversationId = await secondResponse.Content.ReadFromJsonAsync<Guid>();
        secondConversationId.Should().Be(firstConversationId);
    }
}