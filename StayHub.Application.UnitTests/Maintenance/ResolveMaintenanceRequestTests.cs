using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Maintenance.ResolveMaintenanceRequest;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.UnitTests.Maintenance;

public class ResolveMaintenanceRequestTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly ResolveMaintenanceRequestCommandHandler _handler;

    private readonly IMaintenanceRequestRepository _maintenanceRequestRepositoryMock =
        Substitute.For<IMaintenanceRequestRepository>();

    private readonly IApartmentStaffAssignmentRepository _staffAssignmentRepositoryMock =
        Substitute.For<IApartmentStaffAssignmentRepository>();

    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public ResolveMaintenanceRequestTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new ResolveMaintenanceRequestCommandHandler(
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
        var result = await _handler.Handle(new ResolveMaintenanceRequestCommand(requestId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var request = MaintenanceRequestData.CreateAndStart(Guid.CreateVersion7());
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(request.ApartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new ResolveMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNeitherOwnerAdminNorActiveStaff()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.CreateAndStart(apartment.Id);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);
        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        var result = await _handler.Handle(new ResolveMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_ResolveAndSaveChanges_WhenCallerIsOwner()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.CreateAndStart(apartment.Id);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new ResolveMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(MaintenanceRequestStatus.Resolved);
        request.ResolvedOnUtc.Should().Be(UtcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRequestStillOpen()
    {
        // Arrange — must be started (InProgress) before it can be resolved.
        var apartment = ApartmentData.Create();
        var request = MaintenanceRequestData.Create(apartment.Id);
        _maintenanceRequestRepositoryMock.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new ResolveMaintenanceRequestCommand(request.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotInProgress);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}