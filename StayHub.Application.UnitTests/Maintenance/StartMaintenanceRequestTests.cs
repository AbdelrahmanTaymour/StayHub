using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Maintenance.StartMaintenanceRequest;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.UnitTests.Maintenance;

public class StartMaintenanceRequestTests
{
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly StartMaintenanceRequestCommandHandler _handler;

    private readonly IMaintenanceRequestRepository _maintenanceRequestRepositoryMock =
        Substitute.For<IMaintenanceRequestRepository>();

    private readonly IApartmentStaffAssignmentRepository _staffAssignmentRepositoryMock =
        Substitute.For<IApartmentStaffAssignmentRepository>();

    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public StartMaintenanceRequestTests()
    {
        _handler = new StartMaintenanceRequestCommandHandler(
            _maintenanceRequestRepositoryMock,
            _apartmentRepositoryMock,
            _staffAssignmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock);
    }

    private void SetUpNoActiveStaff(Guid apartmentId) =>
        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartmentId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRequestNotFound()
    {
        // Arrange
        var requestId = Guid.CreateVersion7();
        _maintenanceRequestRepositoryMock.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
            .Returns((MaintenanceRequest?)null);

        // Act
        var result = await _handler.Handle(new StartMaintenanceRequestCommand(requestId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var request = MaintenanceRequestData.Create(Guid.CreateVersion7());
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(request.ApartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new StartMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNeitherOwnerAdminNorActiveStaff()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.Create(apartment.Id);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);
        SetUpNoActiveStaff(apartment.Id);

        // Act
        var result = await _handler.Handle(new StartMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_StartAndSaveChanges_WhenCallerIsOwner()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.Create(apartment.Id);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new StartMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(MaintenanceRequestStatus.InProgress);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Start_WhenCallerIsActiveStaff()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.Create(apartment.Id);
        var staffUserId = Guid.CreateVersion7();
        var staffAssignment = ApartmentStaffAssignment.Create(apartment.Id, staffUserId,
            ApartmentStaffRole.MaintenanceStaff, DateTime.UtcNow);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(staffUserId);
        _userContextMock.Roles.Returns([]);
        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, staffUserId, Arg.Any<CancellationToken>())
            .Returns(staffAssignment);

        // Act
        var result = await _handler.Handle(new StartMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(MaintenanceRequestStatus.InProgress);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRequestNotOpen()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.CreateAndStart(apartment.Id);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new StartMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotOpen);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}