using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Maintenance.GetMaintenanceRequestsByApartment;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Maintenance;

public sealed class GetMaintenanceRequestsByApartmentTests(IntegrationTestWebAppFactory factory)
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
        var result = await Sender.Send(new GetMaintenanceRequestsByApartmentQuery(apartment.Id, null, 1, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().HaveCount(2);

        result.Value[0].Id.Should().Be(newerRequest.Id);
        result.Value[1].Id.Should().Be(olderRequest.Id);
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
            new GetMaintenanceRequestsByApartmentQuery(apartment.Id, null, 1, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Id.Should().Be(maintenanceRequest.Id);
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
            new GetMaintenanceRequestsByApartmentQuery(apartment.Id, null, 1, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Id.Should().Be(maintenanceRequest.Id);
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
            new GetMaintenanceRequestsByApartmentQuery(apartment.Id, null, 1, 10));

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
            new GetMaintenanceRequestsByApartmentQuery(apartmentId, null, 1, 10));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
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
            new GetMaintenanceRequestsByApartmentQuery(apartment.Id, MaintenanceRequestStatus.Resolved, 1, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().ContainSingle();

        result.Value[0].Id.Should().Be(resolvedRequest.Id);
        result.Value[0].Status.Should().Be(MaintenanceRequestStatus.Resolved);
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
            new GetMaintenanceRequestsByApartmentQuery(apartment.Id, null, 1, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
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
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-1)),
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-2)),
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-3)),
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-4)),
            MaintenanceRequestTestData.Create(apartment.Id, owner.Id, createdOnUtc: now.AddMinutes(-5))
        };

        DbContext.AddRange(owner, apartment);
        DbContext.AddRange(requests);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetMaintenanceRequestsByApartmentQuery(apartment.Id, null, 2, 2));

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().HaveCount(2);

        result.Value[0].Id.Should().Be(requests[2].Id);
        result.Value[1].Id.Should().Be(requests[3].Id);
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
            new GetMaintenanceRequestsByApartmentQuery(apartment.Id, null, 1, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().HaveCount(3);

        result.Value[0].Id.Should().Be(requests[0].Id);
        result.Value[1].Id.Should().Be(requests[1].Id);
        result.Value[2].Id.Should().Be(requests[2].Id);
    }
}