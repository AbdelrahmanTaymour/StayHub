using FluentAssertions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Apartments.Events;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Apartments;

public class ApartmentStaffAssignmentTests : BaseTest
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var apartmentId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        // Act
        var assignment = ApartmentStaffAssignment.Create(
            apartmentId,
            userId,
            ApartmentStaffRole.Manager,
            utcNow);

        // Assert
        assignment.Id.Should().NotBeEmpty();
        assignment.ApartmentId.Should().Be(apartmentId);
        assignment.UserId.Should().Be(userId);
        assignment.Role.Should().Be(ApartmentStaffRole.Manager);
        assignment.RevokedOnUtc.Should().BeNull();
        assignment.CreatedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Create_Should_RaiseApartmentStaffAssignmentCreatedDomainEvent()
    {
        // Act
        var assignment = ApartmentData.CreateStaffAssignment();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentStaffAssignmentCreatedDomainEvent>(assignment);
        domainEvent.Id.Should().Be(assignment.Id);
        domainEvent.ApartmentId.Should().Be(assignment.ApartmentId);
        domainEvent.UserId.Should().Be(assignment.UserId);
    }

    [Fact]
    public void ChangeRole_Should_UpdateRole_WhenNotRevoked()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment(role: ApartmentStaffRole.Cleaner);

        // Act
        var result = assignment.ChangeRole(ApartmentStaffRole.Manager);

        // Assert
        result.IsSuccess.Should().BeTrue();
        assignment.Role.Should().Be(ApartmentStaffRole.Manager);
    }

    [Fact]
    public void ChangeRole_Should_ReturnFailure_WhenAlreadyRevoked()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment();
        assignment.Revoke(DateTime.UtcNow);

        // Act
        var result = assignment.ChangeRole(ApartmentStaffRole.Manager);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentStaffAssignmentErrors.AlreadyRevoked);
    }

    [Fact]
    public void ChangeRole_Should_NotChangeRole_WhenAlreadyRevoked()
    {
        // Arrange — a revoked assignment's role should stay frozen at
        // whatever it was when revoked, not silently update.
        var assignment = ApartmentData.CreateStaffAssignment(role: ApartmentStaffRole.Cleaner);
        assignment.Revoke(DateTime.UtcNow);

        // Act
        assignment.ChangeRole(ApartmentStaffRole.Manager);

        // Assert
        assignment.Role.Should().Be(ApartmentStaffRole.Cleaner);
    }

    [Fact]
    public void Revoke_Should_SetRevokedOnUtcAndReturnSuccess()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = assignment.Revoke(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        assignment.RevokedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Revoke_Should_RaiseApartmentStaffAssignmentRevokedDomainEvent()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment();
        assignment.ClearDomainEvents();

        // Act
        assignment.Revoke(DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentStaffAssignmentRevokedDomainEvent>(assignment);
        domainEvent.ApartmentStaffAssignmentId.Should().Be(assignment.Id);
    }


    [Fact]
    public void Revoke_Should_ReturnFailure_WhenAlreadyRevoked()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment();
        assignment.Revoke(DateTime.UtcNow);

        // Act
        var result = assignment.Revoke(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentStaffAssignmentErrors.AlreadyRevoked);
    }

    [Fact]
    public void Revoke_Should_NotRaiseDomainEventAgain_WhenAlreadyRevoked()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment();
        assignment.Revoke(DateTime.UtcNow);

        // Act
        assignment.Revoke(DateTime.UtcNow);

        // Assert
        AssertDomainEventWasPublishedTimes<ApartmentStaffAssignmentRevokedDomainEvent>(assignment, 1);
    }
}