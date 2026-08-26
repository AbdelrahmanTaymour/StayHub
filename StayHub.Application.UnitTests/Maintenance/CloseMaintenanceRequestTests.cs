using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Maintenance.CloseMaintenanceRequest;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.UnitTests.Maintenance;

public class CloseMaintenanceRequestTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly CloseMaintenanceRequestCommandHandler _handler;

    private readonly IMaintenanceRequestRepository _maintenanceRequestRepositoryMock =
        Substitute.For<IMaintenanceRequestRepository>();

    private readonly IApartmentStaffAssignmentRepository _staffAssignmentRepositoryMock =
        Substitute.For<IApartmentStaffAssignmentRepository>();

    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public CloseMaintenanceRequestTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new CloseMaintenanceRequestCommandHandler(
            _maintenanceRequestRepositoryMock,
            _apartmentRepositoryMock,
            _staffAssignmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRequestNotFound()
    {
        // Arrange
        var requestId = Guid.CreateVersion7();
        _maintenanceRequestRepositoryMock.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
            .Returns((MaintenanceRequest?)null);

        // Act
        var result = await _handler.Handle(new CloseMaintenanceRequestCommand(requestId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateStartAndResolve(Guid.CreateVersion7());
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(request.ApartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new CloseMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNeitherOwnerAdminNorActiveStaff()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.CreateStartAndResolve(apartment.Id);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);
        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        var result = await _handler.Handle(new CloseMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_CloseAndSaveChanges_WhenCallerIsOwner()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.CreateStartAndResolve(apartment.Id);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new CloseMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(MaintenanceRequestStatus.Closed);
        request.ClosedOnUtc.Should().Be(UtcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Close_WhenCallerIsActiveStaff()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.CreateStartAndResolve(apartment.Id);
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
        var result = await _handler.Handle(new CloseMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRequestNotResolved()
    {
        // Arrange — must be resolved before it can be closed; can't skip
        // straight from InProgress to Closed.
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.CreateAndStart(apartment.Id);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new CloseMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotResolved);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}