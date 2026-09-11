using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Conversations;

public sealed class GetMyConversationsTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task GetMyConversations_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await HttpClient.GetAsync(ConversationRoutes.BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyConversations_ShouldReturnEmptyList_WhenCallerHasNoConversations()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(ConversationRoutes.BaseRoute);
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyConversations_ShouldReturnConversationsWhereCallerIsGuestOrOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var startResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(apartmentId));
        startResponse.EnsureSuccessStatusCode();

        // Act
        AuthenticateAs(ownerToken);
        var ownerViewResponse = await HttpClient.GetAsync(ConversationRoutes.BaseRoute);
        ownerViewResponse.EnsureSuccessStatusCode();

        // Assert
        var ownerResults = await ownerViewResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        ownerResults.Should().ContainSingle();
    }

    [Fact]
    public async Task GetMyConversations_ShouldNotReturnConversations_WhenCallerIsNotPartOf()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var startResponse = await HttpClient.PostAsJsonAsync(
            ConversationRoutes.BaseRoute, ConversationTestData.ValidStartRequest(apartmentId));
        startResponse.EnsureSuccessStatusCode();

        var (unrelatedToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(unrelatedToken);

        // Act
        var response = await HttpClient.GetAsync(ConversationRoutes.BaseRoute);
        response.EnsureSuccessStatusCode();

        // Assert
        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        results.Should().BeEmpty();
    }
}