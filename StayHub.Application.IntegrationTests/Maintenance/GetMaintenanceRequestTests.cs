using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Maintenance.GetMaintenanceRequest;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Maintenance;

public class GetMaintenanceRequestTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetMaintenanceRequest_ShouldReturnExpectedResponse_WhenCallerIsOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, apartment);
        await DbContext.SaveChangesAsync();

        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.Add(maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var query = new GetMaintenanceRequestQuery(maintenanceRequest.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(maintenanceRequest.Id);
        result.Value.ApartmentId.Should().Be(apartment.Id);
        result.Value.ApartmentName.Should().Be(apartment.Name.Value);
        result.Value.Title.Should().Be(maintenanceRequest.Title.Value);
        result.Value.Description.Should().Be(maintenanceRequest.Description.Value);
        result.Value.Status.Should().Be(MaintenanceRequestStatus.Open);
        result.Value.ReportedByUserId.Should().Be(reporter.Id);
        result.Value.ReporterFirstName.Should().Be(reporter.FirstName.Value);
        result.Value.ReporterLastName.Should().Be(reporter.LastName.Value);
        result.Value.ReporterEmail.Should().Be(reporter.Email.Value);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldReturnNotFound_WhenMaintenanceRequestDoesNotExist()
    {
        // Arrange
        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        var query = new GetMaintenanceRequestQuery(Guid.CreateVersion7());

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotFound);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldReturnNotAuthorized_WhenCallerIsNotOwnerAdminReporterOrActiveStaff()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var unrelatedUser = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, unrelatedUser, apartment);

        await DbContext.SaveChangesAsync();

        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.Add(maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(unrelatedUser.Id, Role.Guest.Name);

        var query = new GetMaintenanceRequestQuery(maintenanceRequest.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldSucceed_WhenCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, apartment);
        await DbContext.SaveChangesAsync();

        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.Add(maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(new GetMaintenanceRequestQuery(maintenanceRequest.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(maintenanceRequest.Id);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldSucceed_WhenCallerIsReporter()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, apartment);
        await DbContext.SaveChangesAsync();

        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.Add(maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(reporter.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMaintenanceRequestQuery(maintenanceRequest.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(maintenanceRequest.Id);
    }

    [Fact]
    public async Task GetMaintenanceRequest_ShouldSucceed_WhenCallerIsActiveStaff()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var staffUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, staffUser, apartment);
        await DbContext.SaveChangesAsync();

        var staffRole = Enum.GetValues<ApartmentStaffRole>().First();
        var assignment = ApartmentStaffAssignment.Create(apartment.Id, staffUser.Id, staffRole, DateTime.UtcNow);
        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.AddRange(assignment, maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(staffUser.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMaintenanceRequestQuery(maintenanceRequest.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(maintenanceRequest.Id);
    }
}