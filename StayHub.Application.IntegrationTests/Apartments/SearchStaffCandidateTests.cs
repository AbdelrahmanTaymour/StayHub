using FluentAssertions;
using StayHub.Application.Apartments.SearchStaffCandidate;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

public class SearchStaffCandidateTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Search_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new SearchStaffCandidateQuery(Guid.CreateVersion7(), "someone@example.com"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Search_ShouldReturnNotFound_WhenCallerIsNotTheOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var candidate = UserTestData.CreateUser(email: "hiroshi.tanaka@kyotoclean.jp");
        DbContext.AddRange(owner, apartment, candidate);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new SearchStaffCandidateQuery(apartment.Id, "hiroshi.tanaka@kyotoclean.jp"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Search_ShouldReturnUserNotFound_WhenNoAccountMatchesTheEmail()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new SearchStaffCandidateQuery(apartment.Id, "cleaner.sub@gmail.com"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Search_ShouldReturnCandidateDetails_WhenUserExistsAndIsNotAssignedOrOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var candidate = UserTestData.CreateUser(
            firstName: "Hiroshi", lastName: "Tanaka", email: "hiroshi.tanaka@kyotoclean.jp");
        DbContext.AddRange(owner, apartment, candidate);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new SearchStaffCandidateQuery(apartment.Id, "hiroshi.tanaka@kyotoclean.jp"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(candidate.Id);
        result.Value.FullName.Should().Be("Hiroshi Tanaka");
        result.Value.Email.Should().Be("hiroshi.tanaka@kyotoclean.jp");
        result.Value.IsApartmentOwner.Should().BeFalse();
        result.Value.IsAlreadyAssigned.Should().BeFalse();
        result.Value.CurrentRole.Should().BeNull();
    }

    [Fact]
    public async Task Search_ShouldReturnIsApartmentOwnerTrue_WhenSearchedEmailBelongsToTheOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser(email: "kenji@kyotostays.jp");
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new SearchStaffCandidateQuery(apartment.Id, "kenji@kyotostays.jp"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsApartmentOwner.Should().BeTrue();
    }

    [Fact]
    public async Task Search_ShouldReturnIsAlreadyAssignedTrueAndCurrentRole_WhenAnActiveAssignmentExists()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var candidate = UserTestData.CreateUser(email: "akane.suzuki@kyotoclean.jp");
        DbContext.AddRange(owner, apartment, candidate);
        await DbContext.SaveChangesAsync();

        var assignment = ApartmentStaffAssignment.Create(
            apartment.Id, candidate.Id, ApartmentStaffRole.Cleaner, DateTime.UtcNow);
        DbContext.Add(assignment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new SearchStaffCandidateQuery(apartment.Id, "akane.suzuki@kyotoclean.jp"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAlreadyAssigned.Should().BeTrue();
        result.Value.CurrentRole.Should().Be(ApartmentStaffRole.Cleaner);
    }

    [Fact]
    public async Task Search_ShouldReturnIsAlreadyAssignedFalse_WhenTheOnlyAssignmentHasBeenRevoked()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var candidate = UserTestData.CreateUser(email: "akane.suzuki@kyotoclean.jp");
        DbContext.AddRange(owner, apartment, candidate);
        await DbContext.SaveChangesAsync();

        var assignment = ApartmentStaffAssignment.Create(
            apartment.Id, candidate.Id, ApartmentStaffRole.Cleaner, DateTime.UtcNow);
        assignment.Revoke(DateTime.UtcNow);
        DbContext.Add(assignment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new SearchStaffCandidateQuery(apartment.Id, "akane.suzuki@kyotoclean.jp"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAlreadyAssigned.Should().BeFalse();
        result.Value.CurrentRole.Should().BeNull();
    }

    [Fact]
    public async Task Search_ShouldReturnCandidateDetails_WhenCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var candidate = UserTestData.CreateUser(email: "hiroshi.tanaka@kyotoclean.jp");
        DbContext.AddRange(owner, apartment, candidate);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(
            new SearchStaffCandidateQuery(apartment.Id, "hiroshi.tanaka@kyotoclean.jp"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(candidate.Id);
    }

    [Fact]
    public async Task Search_ShouldNotReturnAnotherApartmentsAssignment_AsAlreadyAssigned()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment A");
        var otherApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment B");
        var candidate = UserTestData.CreateUser(email: "hiroshi.tanaka@kyotoclean.jp");
        DbContext.AddRange(owner, apartment, otherApartment, candidate);
        await DbContext.SaveChangesAsync();

        var assignmentOnOtherApartment = ApartmentStaffAssignment.Create(
            otherApartment.Id, candidate.Id, ApartmentStaffRole.Manager, DateTime.UtcNow);
        DbContext.Add(assignmentOnOtherApartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new SearchStaffCandidateQuery(apartment.Id, "hiroshi.tanaka@kyotoclean.jp"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAlreadyAssigned.Should().BeFalse();
    }
}