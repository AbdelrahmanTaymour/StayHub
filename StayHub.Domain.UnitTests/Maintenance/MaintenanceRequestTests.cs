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

        // Act
        var result = request.Start();

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(MaintenanceRequestStatus.InProgress);
    }

    [Fact]
    public void Start_Should_RaiseMaintenanceRequestStartedDomainEvent_WhenOpen()
    {
        // Arrange
        var request = MaintenanceRequestData.Create();

        // Act
        request.Start();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<MaintenanceRequestStartedDomainEvent>(request);
        domainEvent.MaintenanceRequestId.Should().Be(request.Id);
    }

    [Fact]
    public void Start_Should_ReturnFailure_WhenAlreadyInProgress()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateAndStart();

        // Act
        var result = request.Start();

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
        // Arrange — must be resolved before it can be closed; can't skip
        // straight from InProgress to Closed.
        var request = MaintenanceRequestData.CreateAndStart();

        // Act
        var result = request.Close(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotResolved);
    }
}