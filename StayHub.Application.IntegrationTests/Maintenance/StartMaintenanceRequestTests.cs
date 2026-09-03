using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Maintenance.StartMaintenanceRequest;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Maintenance;

public class StartMaintenanceRequestTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task StartMaintenanceRequest_ShouldSucceed_WhenCallerIsAnActiveStaffAssignee()
    {
        // Arrange — real staffAssignmentRepository.GetActiveAsync path, not
        // owner/admin.
        var owner = UserTestData.CreateUser();
        var staffUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, staffUser, apartment);
        await DbContext.SaveChangesAsync();

        var staffRole = Enum.GetValues<ApartmentStaffRole>().First();
        var assignment = ApartmentStaffAssignment.Create(apartment.Id, staffUser.Id, staffRole, DateTime.UtcNow);
        DbContext.Add(assignment);
        await DbContext.SaveChangesAsync();

        var maintenanceRequest = MaintenanceRequest.Create(
            apartment.Id, owner.Id, new Title("Broken AC"), new Description("The AC unit stopped working."),
            DateTime.UtcNow);
        DbContext.Add(maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(staffUser.Id, Role.Guest.Name);

        var command = new StartMaintenanceRequestCommand(maintenanceRequest.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Set<MaintenanceRequest>().SingleAsync(r => r.Id == maintenanceRequest.Id);
        persisted.Status.Should().Be(MaintenanceRequestStatus.InProgress);
    }

    [Fact]
    public async Task StartMaintenanceRequest_ShouldReturnNotAuthorized_WhenCallerIsNotOwnerAdminOrActiveStaff()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var unrelatedUser = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, unrelatedUser, apartment);
        await DbContext.SaveChangesAsync();

        var maintenanceRequest = MaintenanceRequest.Create(
            apartment.Id, owner.Id, new Title("Broken AC"), new Description("The AC unit stopped working."),
            DateTime.UtcNow);
        DbContext.Add(maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(unrelatedUser.Id, Role.Guest.Name);

        var command = new StartMaintenanceRequestCommand(maintenanceRequest.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }
}