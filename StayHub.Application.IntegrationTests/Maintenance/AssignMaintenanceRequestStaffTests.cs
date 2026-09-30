using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Maintenance.AssignMaintenanceRequestStaff;
using StayHub.Application.Maintenance.GetMaintenanceRequest;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Maintenance;

public class AssignMaintenanceRequestStaffTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task AssignStaff_ShouldSucceed_WhenCallerIsOwnerAndAssigneeIsActiveStaff()
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

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUser.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await Sender.Send(new GetMaintenanceRequestQuery(maintenanceRequest.Id));
        updated.Value.AssignedToUserId.Should().Be(staffUser.Id);
    }

    [Fact]
    public async Task AssignStaff_ShouldSucceed_WhenCallerIsAdmin()
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

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUser.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnNotFound_WhenMaintenanceRequestDoesNotExist()
    {
        // Arrange
        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(
            new AssignMaintenanceRequestStaffCommand(Guid.CreateVersion7(), Guid.CreateVersion7()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotFound);
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnNotAuthorized_WhenCallerIsUnrelatedUser()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var staffUser = UserTestData.CreateUser();
        var unrelatedUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, staffUser, unrelatedUser, apartment);
        await DbContext.SaveChangesAsync();

        var staffRole = Enum.GetValues<ApartmentStaffRole>().First();
        var assignment = ApartmentStaffAssignment.Create(apartment.Id, staffUser.Id, staffRole, DateTime.UtcNow);
        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.AddRange(assignment, maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(unrelatedUser.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUser.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnNotAuthorized_WhenCallerIsActiveStaffButNotOwnerOrAdmin()
    {
        // Being active staff is enough to be assigned, but not enough to
        // assign someone else — that's an owner/admin action.
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var staffUser = UserTestData.CreateUser();
        var otherStaffUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, staffUser, otherStaffUser, apartment);
        await DbContext.SaveChangesAsync();

        var staffRole = Enum.GetValues<ApartmentStaffRole>().First();
        var callerAssignment = ApartmentStaffAssignment.Create(apartment.Id, staffUser.Id, staffRole, DateTime.UtcNow);
        var targetAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, otherStaffUser.Id, staffRole, DateTime.UtcNow);
        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.AddRange(callerAssignment, targetAssignment, maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(staffUser.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, otherStaffUser.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnAssigneeIsNotActiveStaff_WhenTargetUserIsNotStaffForThisApartment()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var randomUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, randomUser, apartment);
        await DbContext.SaveChangesAsync();

        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.Add(maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, randomUser.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.AssigneeIsNotActiveStaff);
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnAssigneeIsNotActiveStaff_WhenTargetIsStaffForADifferentApartment()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var staffUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var otherApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, staffUser, apartment, otherApartment);
        await DbContext.SaveChangesAsync();

        var staffRole = Enum.GetValues<ApartmentStaffRole>().First();
        // Staff is assigned to otherApartment, not the apartment this ticket belongs to.
        var assignment = ApartmentStaffAssignment.Create(otherApartment.Id, staffUser.Id, staffRole, DateTime.UtcNow);
        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.AddRange(assignment, maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUser.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.AssigneeIsNotActiveStaff);
    }

    [Fact]
    public async Task AssignStaff_ShouldReturnAlreadyClosed_WhenTicketIsClosed()
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
        var maintenanceRequest = MaintenanceRequestTestData.CreateStartAndResolve(apartment.Id, reporter.Id);
        maintenanceRequest.Close(DateTime.UtcNow);

        DbContext.AddRange(assignment, maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUser.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.AlreadyClosed);
    }

    [Fact]
    public async Task AssignStaff_ShouldOverwritePreviousAssignee_WhenReassigned()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var firstStaffUser = UserTestData.CreateUser();
        var secondStaffUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, firstStaffUser, secondStaffUser, apartment);
        await DbContext.SaveChangesAsync();

        var staffRole = Enum.GetValues<ApartmentStaffRole>().First();
        var firstAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, firstStaffUser.Id, staffRole, DateTime.UtcNow);
        var secondAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, secondStaffUser.Id, staffRole, DateTime.UtcNow);
        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.AddRange(firstAssignment, secondAssignment, maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        await Sender.Send(new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, firstStaffUser.Id));

        // Act
        var result = await Sender.Send(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, secondStaffUser.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await Sender.Send(new GetMaintenanceRequestQuery(maintenanceRequest.Id));
        updated.Value.AssignedToUserId.Should().Be(secondStaffUser.Id);
    }
}