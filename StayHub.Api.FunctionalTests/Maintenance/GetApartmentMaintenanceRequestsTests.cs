using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StayHub.Api.FunctionalTests.Apartments;
using StayHub.Api.FunctionalTests.Bookings;
using StayHub.Api.FunctionalTests.Infrastructure;
using StayHub.Domain.Apartments;

namespace StayHub.Api.FunctionalTests.Maintenance;

public sealed class GetApartmentMaintenanceRequestsTests(FunctionalTestWebAppFactory factory)
    : BaseFunctionalTest(factory)
{
    private async Task<Guid> CreateOpenRequestAsync(Guid apartmentId, string? title = null)
    {
        var response = await HttpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest(title: title));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnOk_WhenCallerIsOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await CreateOpenRequestAsync(apartmentId);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnOk_WhenCallerIsAdmin()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await CreateOpenRequestAsync(apartmentId);

        var (adminToken, _, adminUserId) = await RegisterAndAuthenticateAsync();
        await Factory.PromoteToAdminAsync(adminUserId);
        AuthenticateAs(adminToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnOk_WhenCallerIsActiveStaff()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await CreateOpenRequestAsync(apartmentId);

        var (staffToken, _, staffUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var assignResponse = await HttpClient.PostAsJsonAsync(
            ApartmentRoutes.Staff(apartmentId),
            new { StaffUserId = staffUserId, Role = ApartmentStaffRole.MaintenanceStaff });
        assignResponse.EnsureSuccessStatusCode();

        AuthenticateAs(staffToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnForbidden_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await CreateOpenRequestAsync(apartmentId);

        var (otherToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(otherToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaintenanceRequest.NotAuthorized");
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnForbidden_WhenCallerIsAGuestWhoOnlyReportedOneTicket()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var (guestToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        await CreateOpenRequestAsync(apartmentId);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var (accessToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(accessToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnEmptyPagedEnvelope_WhenApartmentHasNoRequests()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetByApartment_ShouldFilterByStatus_WhenStatusProvided()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var openRequestId = await CreateOpenRequestAsync(apartmentId, title: "Open ticket");
        var inProgressRequestId = await CreateOpenRequestAsync(apartmentId, title: "In-progress ticket");
        var startResponse = await HttpClient.PostAsync(MaintenanceRoutes.Start(inProgressRequestId), null);
        startResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId, "status=Open"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(1);
        result.Items[0].GetProperty("id").GetGuid().Should().Be(openRequestId);
        result.Items[0].GetProperty("status").GetString().Should().Be("Open");
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnAllStatuses_WhenStatusNotProvided()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        await CreateOpenRequestAsync(apartmentId);
        var inProgressRequestId = await CreateOpenRequestAsync(apartmentId);
        var startResponse = await HttpClient.PostAsync(MaintenanceRoutes.Start(inProgressRequestId), null);
        startResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnExpectedShape_WhenRequestsExist()
    {
        // Arrange
        var (ownerToken, _, reporterUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        var requestId = await CreateOpenRequestAsync(apartmentId, title: "Leaky faucet");

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
        result.Page.Should().Be(1);
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);

        var item = result.Items[0];
        item.GetProperty("id").GetGuid().Should().Be(requestId);
        item.GetProperty("title").GetString().Should().Be("Leaky faucet");
        item.GetProperty("status").GetString().Should().Be("Open");
        item.GetProperty("reportedByUserId").GetGuid().Should().Be(reporterUserId);
        item.GetProperty("isReportedByOwner").GetBoolean().Should().BeTrue();
        item.TryGetProperty("createdOnUtc", out _).Should().BeTrue();
        item.TryGetProperty("reporterFirstName", out _).Should().BeTrue();
        item.TryGetProperty("reporterLastName", out _).Should().BeTrue();
        item.TryGetProperty("reporterAvatarUrl", out _).Should().BeTrue();
        item.TryGetProperty("description", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetByApartment_ShouldFlagGuestReportedRequests_AsNotReportedByOwner()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        var ownerRequestId = await CreateOpenRequestAsync(apartmentId, title: "Owner reported");

        var (guestToken, _, guestUserId) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(guestToken);
        var reserveResponse = await HttpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId));
        reserveResponse.EnsureSuccessStatusCode();
        var guestRequestId = await CreateOpenRequestAsync(apartmentId, title: "Guest reported");

        AuthenticateAs(ownerToken);

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().HaveCount(2);

        var ownerItem = result.Items.Single(i => i.GetProperty("id").GetGuid() == ownerRequestId);
        ownerItem.GetProperty("isReportedByOwner").GetBoolean().Should().BeTrue();

        var guestItem = result.Items.Single(i => i.GetProperty("id").GetGuid() == guestRequestId);
        guestItem.GetProperty("reportedByUserId").GetGuid().Should().Be(guestUserId);
        guestItem.GetProperty("isReportedByOwner").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnRequestedPage_WhenPageAndPageSizeProvided()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var oldestRequestId = await CreateOpenRequestAsync(apartmentId, title: "First");
        await CreateOpenRequestAsync(apartmentId, title: "Second");
        await CreateOpenRequestAsync(apartmentId, title: "Third");

        // Act
        var response = await HttpClient.GetAsync(
            MaintenanceRoutes.ByApartment(apartmentId, "page=2&pageSize=2"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
        result.Items[0].GetProperty("id").GetGuid().Should().Be(oldestRequestId);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetByApartment_ShouldFilterByTitle_CaseInsensitively_WhenSearchProvided()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var faucetRequestId = await CreateOpenRequestAsync(apartmentId, title: "Leaky kitchen faucet");
        await CreateOpenRequestAsync(apartmentId, title: "Broken heater");

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId, "search=FAUCET"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(1);
        result.Items[0].GetProperty("id").GetGuid().Should().Be(faucetRequestId);
    }

    [Fact]
    public async Task GetByApartment_ShouldMatchSearch_AgainstReporterName()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        var requestId = await CreateOpenRequestAsync(apartmentId, title: "Leaky kitchen faucet");

        var allResponse = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId));
        allResponse.EnsureSuccessStatusCode();
        var all = await allResponse.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        var reporterFirstName = all!.Items.Single().GetProperty("reporterFirstName").GetString()!;

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(
            apartmentId, $"search={Uri.EscapeDataString(reporterFirstName.ToUpperInvariant())}"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
        result.Items[0].GetProperty("id").GetGuid().Should().Be(requestId);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnEmptyPagedEnvelope_WhenSearchMatchesNothing()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);
        await CreateOpenRequestAsync(apartmentId, title: "Leaky kitchen faucet");

        // Act
        var response = await HttpClient.GetAsync(
            MaintenanceRoutes.ByApartment(apartmentId, "search=zzz-no-such-ticket"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByApartment_ShouldTreatPercentInSearchAsLiteral()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        await CreateOpenRequestAsync(apartmentId, title: "Leaky kitchen faucet");
        var humidityRequestId = await CreateOpenRequestAsync(apartmentId, title: "100% humidity");

        // Act
        var response = await HttpClient.GetAsync(MaintenanceRoutes.ByApartment(apartmentId, "search=%25"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
        result.Items[0].GetProperty("id").GetGuid().Should().Be(humidityRequestId);
    }

    [Fact]
    public async Task GetByApartment_ShouldApplyStatusAndSearchTogether()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        var openRequestId = await CreateOpenRequestAsync(apartmentId, title: "Sump pump inspection");
        var inProgressRequestId = await CreateOpenRequestAsync(apartmentId, title: "Sump pump replacement");
        var startResponse = await HttpClient.PostAsync(MaintenanceRoutes.Start(inProgressRequestId), null);
        startResponse.EnsureSuccessStatusCode();

        // Act
        var response = await HttpClient.GetAsync(
            MaintenanceRoutes.ByApartment(apartmentId, "status=Open&search=sump"));
        response.EnsureSuccessStatusCode();

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<JsonElement>>();
        result!.Items.Should().ContainSingle();
        result.Items[0].GetProperty("id").GetGuid().Should().Be(openRequestId);
    }

    [Fact]
    public async Task GetByApartment_ShouldReturnBadRequest_WhenStatusIsNotAValidEnumName()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndAuthenticateAsync();
        AuthenticateAs(ownerToken);
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsOwnerAsync(HttpClient);

        // Act
        var response = await HttpClient.GetAsync(
            MaintenanceRoutes.ByApartment(apartmentId, "status=NotAStatus"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record PagedResponseDto<T>(
        List<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}