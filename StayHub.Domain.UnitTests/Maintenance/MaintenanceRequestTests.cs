using FluentAssertions;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Maintenance.Events;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Maintenance;

public class MaintenanceRequestTests : BaseTest
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var reportedByUserId = Guid.CreateVersion7();

        // Act
        var request = MaintenanceRequest.Create(
            apartmentId,
            reportedByUserId,
            MaintenanceRequestData.Title,
            MaintenanceRequestData.Description,
            DateTime.UtcNow);

        // Assert
        request.ApartmentId.Should().Be(apartmentId);
        request.ReportedByUserId.Should().Be(reportedByUserId);
        request.Title.Should().Be(MaintenanceRequestData.Title);
        request.Description.Should().Be(MaintenanceRequestData.Description);
        request.Status.Should().Be(MaintenanceRequestStatus.Open);
    }

    [Fact]
    public void Create_Should_RaiseMaintenanceRequestCreatedDomainEvent()
    {
        // Act
        var request = MaintenanceRequestData.Create();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<MaintenanceRequestCreatedDomainEvent>(request);
        domainEvent.MaintenanceRequestId.Should().Be(request.Id);
    }

    [Fact]
    public void Start_Should_SetStatusInProgressAndReturnSuccess_WhenOpen()
    {
        // Arrange
        var request = MaintenanceRequestData.Create();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = request.Start(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(MaintenanceRequestStatus.InProgress);
        request.StartOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Start_Should_RaiseMaintenanceRequestStartedDomainEvent_WhenOpen()
    {
        // Arrange
        var request = MaintenanceRequestData.Create();
        var utcNow = DateTime.UtcNow;

        // Act
        request.Start(utcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<MaintenanceRequestStartedDomainEvent>(request);
        domainEvent.MaintenanceRequestId.Should().Be(request.Id);
        request.StartOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Start_Should_ReturnFailure_WhenAlreadyInProgress()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var request = MaintenanceRequestData.CreateAndStart(utcNow);

        // Act
        var result = request.Start(utcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotOpen);
    }

    [Fact]
    public void Resolve_Should_SetStatusResolvedAndReturnSuccess_WhenInProgress()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateAndStart();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = request.Resolve(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(MaintenanceRequestStatus.Resolved);
        request.ResolvedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Resolve_Should_RaiseMaintenanceRequestResolvedDomainEvent_WhenInProgress()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateAndStart();

        // Act
        request.Resolve(DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<MaintenanceRequestResolvedDomainEvent>(request);
        domainEvent.MaintenanceRequestId.Should().Be(request.Id);
    }

    [Fact]
    public void Resolve_Should_ReturnFailure_WhenStillOpen()
    {
        // Arrange — can't resolve a request that was never started.
        var request = MaintenanceRequestData.Create();

        // Act
        var result = request.Resolve(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotInProgress);
    }

    [Fact]
    public void Close_Should_SetStatusClosedAndReturnSuccess_WhenResolved()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateStartAndResolve();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = request.Close(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(MaintenanceRequestStatus.Closed);
        request.ClosedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Close_Should_RaiseMaintenanceRequestClosedDomainEvent_WhenResolved()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateStartAndResolve();

        // Act
        request.Close(DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<MaintenanceRequestClosedDomainEvent>(request);
        domainEvent.MaintenanceRequestId.Should().Be(request.Id);
    }

    [Fact]
    public void Close_Should_ReturnFailure_WhenStillInProgress()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateAndStart();

        // Act
        var result = request.Close(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotResolved);
    }

    [Theory]
    [MemberData(nameof(NonClosedRequests))]
    public void AssignStaff_Should_SetAssignedToUserIdAndOnUtcAndReturnSuccess_WhenNotClosed(
        MaintenanceRequest request)
    {
        // Arrange
        var staffUserId = Guid.CreateVersion7();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = request.AssignStaff(staffUserId, utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.AssignedToUserId.Should().Be(staffUserId);
        request.AssignedToOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void AssignStaff_Should_RaiseMaintenanceRequestStaffAssignedDomainEvent_WhenNotClosed()
    {
        // Arrange
        var request = MaintenanceRequestData.Create();
        var staffUserId = Guid.CreateVersion7();

        // Act
        request.AssignStaff(staffUserId, DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<MaintenanceRequestStaffAssignedDomainEvent>(request);
        domainEvent.MaintenanceRequestId.Should().Be(request.Id);
        domainEvent.StaffId.Should().Be(staffUserId);
    }

    [Fact]
    public void AssignStaff_Should_OverwritePreviousAssignee_WhenReassigned()
    {
        // Arrange
        var request = MaintenanceRequestData.Create();
        var firstStaffUserId = Guid.CreateVersion7();
        var secondStaffUserId = Guid.CreateVersion7();

        request.AssignStaff(firstStaffUserId, DateTime.UtcNow);
        var reassignedAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = request.AssignStaff(secondStaffUserId, reassignedAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.AssignedToUserId.Should().Be(secondStaffUserId);
        request.AssignedToOnUtc.Should().Be(reassignedAt);
    }

    [Fact]
    public void AssignStaff_Should_ReturnFailure_WhenClosed()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateStartAndResolve();
        request.Close(DateTime.UtcNow);

        // Act
        var result = request.AssignStaff(Guid.CreateVersion7(), DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.AlreadyClosed);
    }

    [Fact]
    public void AssignStaff_Should_NotChangeAssignee_WhenClosed()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateStartAndResolve();
        request.Close(DateTime.UtcNow);

        // Act
        request.AssignStaff(Guid.CreateVersion7(), DateTime.UtcNow);

        // Assert
        request.AssignedToUserId.Should().BeNull();
        request.AssignedToOnUtc.Should().BeNull();
    }

    public static IEnumerable<object[]> NonClosedRequests()
    {
        yield return [MaintenanceRequestData.Create()];
        yield return [MaintenanceRequestData.CreateAndStart()];
        yield return [MaintenanceRequestData.CreateStartAndResolve()];
    }
}