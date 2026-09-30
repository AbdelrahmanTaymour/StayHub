using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Maintenance.GetApartmentMaintenanceRequests;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Maintenance;

public sealed class GetApartmentMaintenanceRequestsTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Get_ShouldReturnRequests_WhenCallerIsOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var olderRequest = MaintenanceRequestTestData.Create(apartment.Id, owner.Id);
        var newerRequest = MaintenanceRequestTestData.Create(apartment.Id, owner.Id);

        DbContext.AddRange(owner, apartment, olderRequest, newerRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentMaintenanceRequestsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);

        result.Value.Items[0].Id.Should().Be(newerRequest.Id);
        result.Value.Items[1].Id.Should().Be(olderRequest.Id);
    }

    [Fact]
    public async Task Get_ShouldReturnRequests_WhenCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var admin = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(owner.Id);
        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, owner.Id);

        DbContext.AddRange(owner, admin, apartment, maintenanceRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(admin.Id, Role.Admin.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(maintenanceRequest.Id);
    }

    [Fact]
    public async Task Get_ShouldReturnRequests_WhenCallerIsActiveStaff()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var staffUser = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, owner.Id);

        var assignment = ApartmentStaffAssignment.Create(apartment.Id, staffUser.Id,
            ApartmentStaffRole.MaintenanceStaff, DateTime.UtcNow);

        DbContext.AddRange(owner, staffUser, apartment, maintenanceRequest, assignment);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(staffUser.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(maintenanceRequest.Id);
    }

    [Fact]
    public async Task Get_ShouldReturnNotAuthorized_WhenCallerIsNotOwnerAdminOrActiveStaff()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var unrelatedUser = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        DbContext.AddRange(owner, unrelatedUser, apartment);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(unrelatedUser.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var user = UserTestData.CreateUser();

        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartmentId));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Get_ShouldReturnEmptyPage_WhenApartmentHasNoRequests()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        DbContext.AddRange(owner, apartment);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Get_ShouldReturnOnlyRequestsWithRequestedStatus()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var openRequest = MaintenanceRequestTestData.Create(apartment.Id, owner.Id);

        var resolvedRequest = MaintenanceRequestTestData.CreateStartAndResolve(apartment.Id, owner.Id);

        DbContext.AddRange(owner, apartment, openRequest, resolvedRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id, null, MaintenanceRequestStatus.Resolved));

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Items.Should().ContainSingle();
        result.Value.TotalCount.Should().Be(1);

        result.Value.Items[0].Id.Should().Be(resolvedRequest.Id);
        result.Value.Items[0].Status.Should().Be(MaintenanceRequestStatus.Resolved);
    }

    [Fact]
    public async Task Get_ShouldReturnAllRequests_WhenStatusIsNotSpecified()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var openRequest = MaintenanceRequestTestData.Create(apartment.Id, owner.Id);

        var resolvedRequest = MaintenanceRequestTestData.CreateStartAndResolve(apartment.Id, owner.Id);

        DbContext.AddRange(owner, apartment, openRequest, resolvedRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectPage_WhenPaginationIsApplied()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var now = DateTime.UtcNow;

        var requests = new List<MaintenanceRequest>
        {
            MaintenanceRequestTestData.CreateAndStart(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-1)),
            MaintenanceRequestTestData.CreateAndStart(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-2)),
            MaintenanceRequestTestData.CreateAndStart(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-3)),
            MaintenanceRequestTestData.CreateAndStart(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-4)),
            MaintenanceRequestTestData.CreateAndStart(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-5)),
        };

        DbContext.AddRange(owner, apartment);
        DbContext.AddRange(requests);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(
                apartment.Id,
                Status: MaintenanceRequestStatus.InProgress,
                Page: 2,
                PageSize: 2));

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be(requests[2].Id);
        result.Value.Items[1].Id.Should().Be(requests[3].Id);

        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(3);
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-5, 500, 1, 50)]
    public async Task Get_ShouldClampPageAndPageSize_WhenValuesAreOutOfRange(
        int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);
        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, owner.Id);

        DbContext.AddRange(owner, apartment, maintenanceRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id, Search: null, Page: page, PageSize: pageSize));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(expectedPage);
        result.Value.PageSize.Should().Be(expectedPageSize);
        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Get_ShouldReturnRequestsOrderedByCreatedOnDescending()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var now = DateTime.UtcNow;

        var requests = new List<MaintenanceRequest>
        {
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-1)),
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-2)),
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-3))
        };

        DbContext.AddRange(owner, apartment);
        DbContext.AddRange(requests);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Items.Should().HaveCount(3);

        result.Value.Items[0].Id.Should().Be(requests[0].Id);
        result.Value.Items[1].Id.Should().Be(requests[1].Id);
        result.Value.Items[2].Id.Should().Be(requests[2].Id);
    }

    [Fact]
    public async Task Get_ShouldFlagOnlyOwnerReportedRequests_AsReportedByOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var otherReporter = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var now = DateTime.UtcNow;

        var ownerRequest = MaintenanceRequestTestData.Create(
            apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-1));
        var otherRequest = MaintenanceRequestTestData.Create(
            apartment.Id, otherReporter.Id, createdOnUtc: now.AddMinutes(-2));

        DbContext.AddRange(owner, otherReporter, apartment, ownerRequest, otherRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);

        result.Value.Items.Single(r => r.Id == ownerRequest.Id).IsReportedByOwner.Should().BeTrue();
        result.Value.Items.Single(r => r.Id == otherRequest.Id).IsReportedByOwner.Should().BeFalse();
    }

    [Fact]
    public async Task Get_ShouldFilterByTitle_CaseInsensitively_WhenSearchProvided()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var faucetRequest = MaintenanceRequestTestData.Create(
            apartment.Id, owner.Id, title: "Leaky kitchen faucet");
        var heaterRequest = MaintenanceRequestTestData.Create(
            apartment.Id, owner.Id, title: "Broken heater");

        DbContext.AddRange(owner, apartment, faucetRequest, heaterRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id, Search: "FAUCET"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(faucetRequest.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_ShouldMatchSearch_AgainstReporterFirstName_CaseInsensitively()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);
        var maintenanceRequest = MaintenanceRequestTestData.Create(
            apartment.Id, owner.Id, title: "Leaky kitchen faucet");

        DbContext.AddRange(owner, apartment, maintenanceRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Read the reporter's real name back rather than assuming what the test data generates.
        var all = await Sender.Send(new GetApartmentMaintenanceRequestsQuery(apartment.Id));
        var reporterFirstName = all.Value.Items.Single().ReporterFirstName;

        // Act
        var result = await Sender.Send(new GetApartmentMaintenanceRequestsQuery(
            apartment.Id, Search: reporterFirstName.ToUpperInvariant()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(r => r.Id == maintenanceRequest.Id);
    }

    [Fact]
    public async Task Get_ShouldMatchSearch_AgainstReporterFullName()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);
        var maintenanceRequest = MaintenanceRequestTestData.Create(
            apartment.Id, owner.Id, title: "Leaky kitchen faucet");

        DbContext.AddRange(owner, apartment, maintenanceRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var all = await Sender.Send(new GetApartmentMaintenanceRequestsQuery(apartment.Id));
        var reporter = all.Value.Items.Single();
        var fullName = $"{reporter.ReporterFirstName} {reporter.ReporterLastName}";

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id, Search: fullName));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(r => r.Id == maintenanceRequest.Id);
    }

    [Fact]
    public async Task Get_ShouldReturnEmptyPage_WhenSearchMatchesNothing()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);
        var maintenanceRequest = MaintenanceRequestTestData.Create(
            apartment.Id, owner.Id, title: "Leaky kitchen faucet");

        DbContext.AddRange(owner, apartment, maintenanceRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentMaintenanceRequestsQuery(
            apartment.Id, Search: "zzz-no-such-ticket"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData("%", "100% humidity")]
    [InlineData("_", "door_lock jammed")]
    public async Task Get_ShouldTreatSearchWildcardsAsLiteralCharacters(string search, string expectedTitle)
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var requests = new[]
        {
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, title: "Leaky kitchen faucet"),
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, title: "100% humidity"),
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, title: "door_lock jammed")
        };

        DbContext.AddRange(owner, apartment);
        DbContext.AddRange(requests);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentMaintenanceRequestsQuery(apartment.Id, Search: search));

        // Assert — unescaped, "%" and "_" would match every row.
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Title.Should().Be(expectedTitle);
    }

    [Fact]
    public async Task Get_ShouldApplyStatusAndSearchTogether()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        var openRequest = MaintenanceRequestTestData.Create(
            apartment.Id, owner.Id, title: "Sump pump replacement");
        var resolvedRequest = MaintenanceRequestTestData.CreateStartAndResolve(apartment.Id, owner.Id);

        DbContext.AddRange(owner, apartment, openRequest, resolvedRequest);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var resolvedResult = await Sender.Send(new GetApartmentMaintenanceRequestsQuery(
            apartment.Id, Search: "sump", MaintenanceRequestStatus.Resolved, 1, 10));
        var openResult = await Sender.Send(new GetApartmentMaintenanceRequestsQuery(
            apartment.Id, Search: "sump", MaintenanceRequestStatus.Open, 1, 10));

        // Assert
        resolvedResult.IsSuccess.Should().BeTrue();
        resolvedResult.Value.Items.Should().BeEmpty();

        openResult.IsSuccess.Should().BeTrue();
        openResult.Value.Items.Should().ContainSingle(r => r.Id == openRequest.Id);
    }
}