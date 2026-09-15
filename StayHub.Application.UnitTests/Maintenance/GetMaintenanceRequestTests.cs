using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Maintenance.GetMaintenanceRequest;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.UnitTests.Maintenance;

public class GetMaintenanceRequestTests
{
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly GetMaintenanceRequestQueryHandler _handler;

    private readonly IMaintenanceRequestRepository _maintenanceRequestRepositoryMock =
        Substitute.For<IMaintenanceRequestRepository>();

    private readonly ISqlConnectionFactory _sqlConnectionFactoryMock = Substitute.For<ISqlConnectionFactory>();

    private readonly IApartmentStaffAssignmentRepository _staffAssignmentRepositoryMock =
        Substitute.For<IApartmentStaffAssignmentRepository>();

    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public GetMaintenanceRequestTests()
    {
        _handler = new GetMaintenanceRequestQueryHandler(
            _sqlConnectionFactoryMock,
            _maintenanceRequestRepositoryMock,
            _apartmentRepositoryMock,
            _staffAssignmentRepositoryMock,
            _userContextMock);
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
        var result = await _handler.Handle(new GetMaintenanceRequestQuery(maintenanceRequestId), default);

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
        await _handler.Handle(new GetMaintenanceRequestQuery(maintenanceRequestId), default);

        // Assert
        await _apartmentRepositoryMock.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotOpenDatabaseConnection_WhenMaintenanceRequestNotFound()
    {
        // Arrange
        var maintenanceRequestId = Guid.CreateVersion7();

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequestId, Arg.Any<CancellationToken>())
            .Returns((MaintenanceRequest?)null);

        // Act
        await _handler.Handle(new GetMaintenanceRequestQuery(maintenanceRequestId), default);

        // Assert
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
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
        var result = await _handler.Handle(new GetMaintenanceRequestQuery(maintenanceRequest.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_NotOpenDatabaseConnection_WhenApartmentNotFound()
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
        await _handler.Handle(new GetMaintenanceRequestQuery(maintenanceRequest.Id), default);

        // Assert
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerAdminReporterOrActiveStaff()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var reporterId = Guid.CreateVersion7();
        var callerId = Guid.CreateVersion7();

        var maintenanceRequest = MaintenanceRequestData.Create(apartment.Id, reporterId);

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.UserId.Returns(callerId);
        _userContextMock.Roles.Returns([]);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, callerId, Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        var result = await _handler.Handle(new GetMaintenanceRequestQuery(maintenanceRequest.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_NotOpenDatabaseConnection_WhenCallerIsNotAuthorized()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var maintenanceRequest = MaintenanceRequestData.Create(apartment.Id);

        var callerId = Guid.CreateVersion7();

        _maintenanceRequestRepositoryMock
            .GetByIdAsync(maintenanceRequest.Id, Arg.Any<CancellationToken>())
            .Returns(maintenanceRequest);

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.UserId.Returns(callerId);
        _userContextMock.Roles.Returns([]);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, callerId, Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        await _handler.Handle(new GetMaintenanceRequestQuery(maintenanceRequest.Id), default);

        // Assert
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }
}