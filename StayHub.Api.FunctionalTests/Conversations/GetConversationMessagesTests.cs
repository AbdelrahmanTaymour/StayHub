using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Conversations;

public sealed class GetConversationMessagesTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
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
        var (_, guestToken, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(guestToken);

        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnOk_WhenCallerIsTheOwner()
    {
        var (ownerToken, _, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(ownerToken);

        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var conversationId = Guid.NewGuid();

        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnEmptyPagedEnvelope_WhenCallerIsNotAParticipant()
    {
        var (_, _, conversationId) = await ArrangeConversationAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnEmptyPagedEnvelope_WhenConversationDoesNotExist()
    {
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(Guid.NewGuid()));
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnMessagesInDescendingOrderBySentTime()
    {
        var (ownerToken, guestToken, conversationId) = await ArrangeConversationAsync();
        AuthenticateAs(guestToken);
        var secondMessage = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.Messages(conversationId),
            ConversationTestData.ValidSendMessageRequest("Second message"));
        secondMessage.EnsureSuccessStatusCode();

        AuthenticateAs(ownerToken);

        var response = await HttpClient.GetAsync(ConversationRoutes.Messages(conversationId));
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().HaveCount(2);
        result.Items[0].GetProperty("body").GetString().Should().Be("Second message");
        result.TotalCount.Should().Be(2);
    }

    private sealed record PagedResponseDto<T>(
        List<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}