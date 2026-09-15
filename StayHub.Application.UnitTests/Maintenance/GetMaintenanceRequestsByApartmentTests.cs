using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Maintenance.GetMaintenanceRequestsByApartment;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.UnitTests.Maintenance;

public sealed class GetMaintenanceRequestsByApartmentTests
{
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly GetMaintenanceRequestsByApartmentQueryHandler _handler;

    private readonly ISqlConnectionFactory _sqlConnectionFactoryMock =
        Substitute.For<ISqlConnectionFactory>();

    private readonly IApartmentStaffAssignmentRepository _staffAssignmentRepositoryMock =
        Substitute.For<IApartmentStaffAssignmentRepository>();

    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public GetMaintenanceRequestsByApartmentTests()
    {
        _handler = new GetMaintenanceRequestsByApartmentQueryHandler(
            _sqlConnectionFactoryMock,
            _apartmentRepositoryMock,
            _staffAssignmentRepositoryMock,
            _userContextMock);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();

        _apartmentRepositoryMock
            .GetByIdAsync(apartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(
            new GetMaintenanceRequestsByApartmentQuery(apartmentId, null, 1, 10), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldNotCheckStaffAssignment_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();

        _apartmentRepositoryMock
            .GetByIdAsync(apartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        await _handler.Handle(new GetMaintenanceRequestsByApartmentQuery(apartmentId, null, 1, 10), default);

        // Assert
        await _staffAssignmentRepositoryMock.DidNotReceive()
            .GetActiveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotOpenDatabaseConnection_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();

        _apartmentRepositoryMock
            .GetByIdAsync(apartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        await _handler.Handle(new GetMaintenanceRequestsByApartmentQuery(apartmentId, null, 1, 10), default);

        // Assert
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCallerIsNotOwnerAdminOrActiveStaff()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        var callerId = Guid.CreateVersion7();

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.UserId.Returns(callerId);
        _userContextMock.Roles.Returns([]);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, callerId, Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        var result = await _handler.Handle(
            new GetMaintenanceRequestsByApartmentQuery(apartment.Id, null, 1, 10), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_ShouldNotOpenDatabaseConnection_WhenCallerIsNotAuthorized()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        var callerId = Guid.CreateVersion7();

        _apartmentRepositoryMock
            .GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock.UserId.Returns(callerId);
        _userContextMock.Roles.Returns([]);

        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, callerId, Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        await _handler.Handle(
            new GetMaintenanceRequestsByApartmentQuery(apartment.Id, null, 1, 10), default);

        // Assert
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }
}