using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Maintenance.AssignMaintenanceRequestStaff;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.UnitTests.Maintenance;

public class AssignMaintenanceRequestStaffTests
{
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly AssignMaintenanceRequestStaffCommandHandler _handler;

    private readonly IMaintenanceRequestRepository _maintenanceRequestRepositoryMock =
        Substitute.For<IMaintenanceRequestRepository>();

    private readonly IApartmentStaffAssignmentRepository _staffAssignmentRepositoryMock =
        Substitute.For<IApartmentStaffAssignmentRepository>();

    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();

    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public AssignMaintenanceRequestStaffTests()
    {
        _handler = new AssignMaintenanceRequestStaffCommandHandler(
            _maintenanceRequestRepositoryMock,
            _apartmentRepositoryMock,
            _staffAssignmentRepositoryMock,
            _userContextMock,
            _dateTimeProviderMock,
            _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenMaintenanceRequestNotFound()
    {
        // Arrange
        var maintenanceRequestId = Guid.CreateVersion7();

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequestId, Arg.Any<CancellationToken>())
            .Returns((MaintenanceRequest?)null);

        // Act
        var result = await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequestId, Guid.CreateVersion7()), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_NotGetApartment_WhenMaintenanceRequestNotFound()
    {
        // Arrange
        var maintenanceRequestId = Guid.CreateVersion7();

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequestId, Arg.Any<CancellationToken>())
            .Returns((MaintenanceRequest?)null);

        // Act
        await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequestId, Guid.CreateVersion7()), default);

        // Assert
        await _apartmentRepositoryMock.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var maintenanceRequest = MaintenanceRequestData.Create(apartmentId);

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, Guid.CreateVersion7()), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.Create(apartment.Id);

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.IsOwner(apartment.OwnerId).Returns(false);
        _userContextMock.IsAdmin.Returns(false);

        // Act
        var result = await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, Guid.CreateVersion7()), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_NotCheckStaffAssignment_WhenCallerIsNotAuthorized()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.Create(apartment.Id);

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.IsOwner(apartment.OwnerId).Returns(false);
        _userContextMock.IsAdmin.Returns(false);

        // Act
        await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, Guid.CreateVersion7()), default);

        // Assert
        await _staffAssignmentRepositoryMock.DidNotReceive()
            .GetActiveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotSaveChanges_WhenCallerIsNotAuthorized()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.Create(apartment.Id);

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.IsOwner(apartment.OwnerId).Returns(false);
        _userContextMock.IsAdmin.Returns(false);

        // Act
        await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, Guid.CreateVersion7()), default);

        // Assert
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAssigneeIsNotAnActiveStaffMember()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.Create(apartment.Id);
        var staffUserId = Guid.CreateVersion7();

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.IsOwner(apartment.OwnerId).Returns(true);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, staffUserId, Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        var result = await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUserId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.AssigneeIsNotActiveStaff);
    }

    [Fact]
    public async Task Handle_Should_NotSaveChanges_WhenAssigneeIsNotAnActiveStaffMember()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.Create(apartment.Id);
        var staffUserId = Guid.CreateVersion7();

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.IsOwner(apartment.OwnerId).Returns(true);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, staffUserId, Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        await _handler.Handle(new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUserId), default);

        // Assert
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessAndSaveChanges_WhenCallerIsOwnerAndAssigneeIsActiveStaff()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.Create(apartment.Id);
        var staffUserId = Guid.CreateVersion7();
        var staffAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, staffUserId, Enum.GetValues<ApartmentStaffRole>().First(), DateTime.UtcNow);
        var utcNow = DateTime.UtcNow;

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.IsOwner(apartment.OwnerId).Returns(true);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, staffUserId, Arg.Any<CancellationToken>())
            .Returns(staffAssignment);

        _dateTimeProviderMock.UtcNow.Returns(utcNow);

        // Act
        var result = await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUserId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        maintenanceRequest.AssignedToUserId.Should().Be(staffUserId);
        maintenanceRequest.AssignedToOnUtc.Should().Be(utcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenCallerIsAdminAndAssigneeIsActiveStaff()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.Create(apartment.Id);
        var staffUserId = Guid.CreateVersion7();
        var staffAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, staffUserId, Enum.GetValues<ApartmentStaffRole>().First(), DateTime.UtcNow);

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.IsOwner(apartment.OwnerId).Returns(false);
        _userContextMock.IsAdmin.Returns(true);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, staffUserId, Arg.Any<CancellationToken>())
            .Returns(staffAssignment);

        _dateTimeProviderMock.UtcNow.Returns(DateTime.UtcNow);

        // Act
        var result = await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUserId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenMaintenanceRequestIsAlreadyClosed()
    {
        // Arrange — domain-level rule: assignment is blocked once the ticket is Closed.
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.CreateStartAndResolve(apartment.Id);
        maintenanceRequest.Close(DateTime.UtcNow);
        var staffUserId = Guid.CreateVersion7();
        var staffAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, staffUserId, Enum.GetValues<ApartmentStaffRole>().First(), DateTime.UtcNow);

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.IsOwner(apartment.OwnerId).Returns(true);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, staffUserId, Arg.Any<CancellationToken>())
            .Returns(staffAssignment);

        // Act
        var result = await _handler.Handle(
            new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUserId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.AlreadyClosed);
    }

    [Fact]
    public async Task Handle_Should_NotSaveChanges_WhenMaintenanceRequestIsAlreadyClosed()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.CreateStartAndResolve(apartment.Id);
        maintenanceRequest.Close(DateTime.UtcNow);
        var staffUserId = Guid.CreateVersion7();
        var staffAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, staffUserId, Enum.GetValues<ApartmentStaffRole>().First(), DateTime.UtcNow);

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.IsOwner(apartment.OwnerId).Returns(true);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, staffUserId, Arg.Any<CancellationToken>())
            .Returns(staffAssignment);

        // Act
        await _handler.Handle(new AssignMaintenanceRequestStaffCommand(maintenanceRequest.Id, staffUserId), default);

        // Assert
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}