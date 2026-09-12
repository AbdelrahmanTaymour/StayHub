using System.Net;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Maintenance;

public sealed class CloseMaintenanceRequestTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, Guid RequestId)> ArrangeResolvedRequestAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);
        var startResponse = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);
        startResponse.EnsureSuccessStatusCode();
        var resolveResponse = await HttpClient.PostAsync(MaintenanceRoutes.Resolve(requestId), null);
        resolveResponse.EnsureSuccessStatusCode();

        return (ownerToken, requestId);
    }

    [Fact]
    public async Task Close_ShouldReturnNoContent_WhenCallerIsOwnerAndRequestResolved()
    {
        // Arrange
        var (ownerToken, requestId) = await ArrangeResolvedRequestAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Close(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Close_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (_, requestId) = await ArrangeResolvedRequestAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Close(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Close_ShouldReturnNoContent_WhenCallerIsAdmin()
    {
        // Arrange
        var (_, requestId) = await ArrangeResolvedRequestAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Close(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Close_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Close(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Close_ShouldReturnConflict_WhenRequestIsNotResolved()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Close(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.NotResolved");
    }

    [Fact]
    public async Task Close_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Close(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}