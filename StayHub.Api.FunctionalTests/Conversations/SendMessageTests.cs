using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Conversations;

public sealed class SendMessageTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, string GuestToken, Guid ConversationId)> ArrangeConversationAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var startResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(apartmentId));
        startResponse.EnsureSuccessStatusCode();
        var conversationId = await startResponse.Content.ReadFromJsonAsync<Guid>();

        return (ownerToken, guestToken, conversationId);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnCreatedWithId_WhenCallerIsTheGuest()
    {
        // Arrange
        var (_, guestToken, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.Messages(conversationId), ConversationTestData.ValidSendMessageRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnCreated_WhenCallerIsTheOwner()
    {
        // Arrange
        var (ownerToken, _, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.Messages(conversationId), ConversationTestData.ValidSendMessageRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnForbidden_WhenCallerIsNotAParticipant()
    {
        // Arrange
        var (_, _, conversationId) = await ArrangeConversationAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.Messages(conversationId), ConversationTestData.ValidSendMessageRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Message.NotAuthorized");
    }

    [Fact]
    public async Task SendMessage_ShouldReturnNotFound_WhenConversationDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.Messages(Guid.NewGuid()), ConversationTestData.ValidSendMessageRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = ConversationTestData.ValidSendMessageRequest();

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.Messages(Guid.NewGuid()), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnValidationProblem_WhenBodyIsEmpty()
    {
        // Arrange
        var (_, guestToken, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(guestToken);

        var request = ConversationTestData.ValidSendMessageRequest(body: "");

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.Messages(conversationId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnValidationProblem_WhenBodyExceedsMaxLength()
    {
        // Arrange
        var (_, guestToken, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(guestToken);

        var request = ConversationTestData.ValidSendMessageRequest(body: new string('a', 4001));

        // Act
        var response = await HttpClient.PostAsJsonAsync(ConversationRoutes.Messages(conversationId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnNotFound_WhenConversationIdRouteParamIsMalformed()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            "api/v1/conversations/not-a-guid/messages", ConversationTestData.ValidSendMessageRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}