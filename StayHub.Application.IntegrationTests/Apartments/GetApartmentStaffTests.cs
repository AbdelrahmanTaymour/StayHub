using FluentAssertions;
using StayHub.Application.Apartments.GetApartmentStaff;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetApartmentStaffTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetStaff_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentStaffQuery(Guid.CreateVersion7()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetStaff_ShouldReturnNotFound_WhenCallerIsNotTheOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentStaffQuery(apartment.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetStaff_ShouldReturnEmptyList_WhenNoStaffAreAssigned()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentStaffQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStaff_ShouldReturnActiveAssignments_OrderedByAssignedDateAscending()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var firstStaff = UserTestData.CreateUser(firstName: "Hiroshi", lastName: "Tanaka");
        var secondStaff = UserTestData.CreateUser(firstName: "Akane", lastName: "Suzuki");
        DbContext.AddRange(owner, apartment, firstStaff, secondStaff);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;
        var earlierAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, firstStaff.Id, ApartmentStaffRole.Manager, baseTime);
        var laterAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, secondStaff.Id, ApartmentStaffRole.Cleaner, baseTime.AddMinutes(1));

        DbContext.AddRange(earlierAssignment, laterAssignment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentStaffQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].AssignmentId.Should().Be(earlierAssignment.Id);
        result.Value[0].FullName.Should().Be("Hiroshi Tanaka");
        result.Value[0].Role.Should().Be(ApartmentStaffRole.Manager);
        result.Value[1].AssignmentId.Should().Be(laterAssignment.Id);
        result.Value[1].Role.Should().Be(ApartmentStaffRole.Cleaner);
    }

    [Fact]
    public async Task GetStaff_ShouldExcludeRevokedAssignments()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var staffUser = UserTestData.CreateUser();
        DbContext.AddRange(owner, apartment, staffUser);
        await DbContext.SaveChangesAsync();

        var assignment = ApartmentStaffAssignment.Create(
            apartment.Id, staffUser.Id, ApartmentStaffRole.Cleaner, DateTime.UtcNow);
        assignment.Revoke(DateTime.UtcNow);
        DbContext.Add(assignment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentStaffQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStaff_ShouldNotReturnAnotherApartmentsStaff()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment A");
        var otherApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment B");
        var staffUser = UserTestData.CreateUser();
        DbContext.AddRange(owner, apartment, otherApartment, staffUser);
        await DbContext.SaveChangesAsync();

        var assignment = ApartmentStaffAssignment.Create(
            otherApartment.Id, staffUser.Id, ApartmentStaffRole.Cleaner, DateTime.UtcNow);
        DbContext.Add(assignment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentStaffQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStaff_ShouldReturnAssignments_WhenCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var staffUser = UserTestData.CreateUser();
        var assignment = ApartmentStaffAssignment.Create(
            apartment.Id, staffUser.Id, ApartmentStaffRole.Manager, DateTime.UtcNow);
        DbContext.AddRange(owner, apartment, staffUser, assignment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(new GetApartmentStaffQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(s => s.AssignmentId == assignment.Id);
    }
}