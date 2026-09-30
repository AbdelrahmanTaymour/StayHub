using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Maintenance.GetMaintenanceRequestForGuest;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Maintenance;

public class GetMaintenanceRequestForGuestTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetMaintenanceRequestForGuest_ShouldReturnExpectedResponse_WhenCallerIsReporter()
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
        var result = await Sender.Send(new GetMaintenanceRequestForGuestQuery(maintenanceRequest.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(maintenanceRequest.Id);
        result.Value.ApartmentId.Should().Be(apartment.Id);
        result.Value.ApartmentName.Should().Be(apartment.Name.Value);
        result.Value.Title.Should().Be(maintenanceRequest.Title.Value);
        result.Value.Description.Should().Be(maintenanceRequest.Description.Value);
        result.Value.Status.Should().Be(MaintenanceRequestStatus.Open);
        result.Value.StartOnUtc.Should().BeNull();
        result.Value.ResolvedOnUtc.Should().BeNull();
        result.Value.ClosedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task GetMaintenanceRequestForGuest_ShouldReturnNotFound_WhenMaintenanceRequestDoesNotExist()
    {
        // Arrange
        var reporter = UserTestData.CreateUser();
        DbContext.Add(reporter);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(reporter.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMaintenanceRequestForGuestQuery(Guid.CreateVersion7()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotFound);
    }

    [Fact]
    public async Task GetMaintenanceRequestForGuest_ShouldReturnNotAuthorized_WhenCallerIsNotTheReporter()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var otherGuest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, otherGuest, apartment);
        await DbContext.SaveChangesAsync();

        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);

        DbContext.Add(maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(otherGuest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMaintenanceRequestForGuestQuery(maintenanceRequest.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task GetMaintenanceRequestForGuest_ShouldReturnNotAuthorized_WhenCallerIsTheOwner()
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

        // Act
        var result = await Sender.Send(new GetMaintenanceRequestForGuestQuery(maintenanceRequest.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task GetMaintenanceRequestForGuest_ShouldReturnNotAuthorized_WhenCallerIsAdmin()
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
        var result = await Sender.Send(new GetMaintenanceRequestForGuestQuery(maintenanceRequest.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task GetMaintenanceRequestForGuest_ShouldReflectStartOnUtc_AfterTicketHasBeenStarted()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var reporter = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, reporter, apartment);
        await DbContext.SaveChangesAsync();

        var maintenanceRequest = MaintenanceRequestTestData.Create(apartment.Id, reporter.Id);
        var startedAt = DateTime.UtcNow;
        maintenanceRequest.Start(startedAt);

        DbContext.Add(maintenanceRequest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(reporter.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMaintenanceRequestForGuestQuery(maintenanceRequest.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(MaintenanceRequestStatus.InProgress);
        result.Value.StartOnUtc.Should().Be(startedAt);
    }
}