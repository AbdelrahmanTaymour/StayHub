using System.Net;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Infrastructure;

namespace StayHub.Api.FunctionalTests.Maintenance;

public sealed class ResolveMaintenanceRequestTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private async Task<(string OwnerToken, Guid RequestId)> ArrangeInProgressRequestAsync()
    {
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);
        var startResponse = await HttpClient.PostAsync(MaintenanceRoutes.Start(requestId), null);
        startResponse.EnsureSuccessStatusCode();

        return (ownerToken, requestId);
    }

    [Fact]
    public async Task Resolve_ShouldReturnNoContent_WhenCallerIsOwnerAndRequestInProgress()
    {
        // Arrange
        var (ownerToken, requestId) = await ArrangeInProgressRequestAsync();
        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Resolve(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Resolve_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (_, requestId) = await ArrangeInProgressRequestAsync();

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Resolve(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Resolve_ShouldReturnNoContent_WhenCallerIsAdmin()
    {
        // Arrange
        var (_, requestId) = await ArrangeInProgressRequestAsync();

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Resolve(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Resolve_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Resolve(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Resolve_ShouldReturnConflict_WhenRequestIsStillOpen()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var (_, requestId) = await MaintenanceTestFixtures.CreateOpenRequestAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Resolve(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.NotInProgress");
    }

    [Fact]
    public async Task Resolve_ShouldReturnConflict_WhenAlreadyResolved()
    {
        // Arrange
        var (ownerToken, requestId) = await ArrangeInProgressRequestAsync();
        AuthenticateAs(ownerToken);
        var firstResolve = await HttpClient.PostAsync(MaintenanceRoutes.Resolve(requestId), null);
        firstResolve.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Resolve(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Resolve_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync(MaintenanceRoutes.Resolve(requestId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}