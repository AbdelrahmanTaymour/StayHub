using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Conversations;

public sealed class GetMessagesByConversationTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, string GuestToken, Guid ConversationId)> ArrangeConversationAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var startResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(apartmentId));
        startResponse.EnsureSuccessStatusCode();
        var conversationId = await startResponse.Content.ReadFromJsonAsync<Guid>();

        return (ownerToken, guestToken, conversationId);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnOk_WhenCallerIsTheGuest()
    {
        // Arrange
        var (_, guestToken, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(guestToken);

        // Act
        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnOk_WhenCallerIsTheOwner()
    {
        // Arrange
        var (ownerToken, _, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnEmptyList_WhenCallerIsNotAParticipant()
    {
        // Arrange
        var (_, _, conversationId) = await ArrangeConversationAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnEmptyList_WhenConversationDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(Guid.NewGuid()));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnMessagesInDescendingOrderBySentTime()
    {
        // Arrange
        var (ownerToken, guestToken, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(guestToken);
        var secondMessage = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.Messages(conversationId),
            ConversationTestData.ValidSendMessageRequest("Second message"));
        secondMessage.EnsureSuccessStatusCode();

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().HaveCount(2);
        results![0].GetProperty("body").GetString().Should().Be("Second message");
    }
}