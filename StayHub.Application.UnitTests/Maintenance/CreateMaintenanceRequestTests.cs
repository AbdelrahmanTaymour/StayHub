using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Maintenance.CreateMaintenanceRequest;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Maintenance;

public class CreateMaintenanceRequestTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IBookingRepository _bookingRepositoryMock = Substitute.For<IBookingRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly CreateMaintenanceRequestCommandHandler _handler;

    private readonly IMaintenanceRequestRepository _maintenanceRequestRepositoryMock =
        Substitute.For<IMaintenanceRequestRepository>();

    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public CreateMaintenanceRequestTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new CreateMaintenanceRequestCommandHandler(
            _apartmentRepositoryMock,
            _bookingRepositoryMock,
            _maintenanceRequestRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static CreateMaintenanceRequestCommand CommandFor(Guid apartmentId) =>
        new(apartmentId, "Leaking faucet", "The kitchen faucet is leaking");

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(CommandFor(apartmentId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNeitherOwnerAdminNorActiveTenant()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);
        _bookingRepositoryMock
            .HasActiveBookingAsync(apartment.Id, Arg.Any<Guid>(), UtcNow, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceRequestErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_CreateRequest_WhenCallerIsOwner()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _maintenanceRequestRepositoryMock.Received(1).Add(Arg.Is<MaintenanceRequest>(m => m.Id == result.Value));
    }

    [Fact]
    public async Task Handle_Should_CreateRequest_WhenCallerIsAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_CreateRequest_WhenCallerHasActiveBooking()
    {
        // Arrange — a guest currently staying at the apartment, reporting
        // an issue during their stay.
        var apartment = ApartmentData.Create();
        var tenantId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(tenantId);
        _userContextMock.Roles.Returns([]);
        _bookingRepositoryMock
            .HasActiveBookingAsync(apartment.Id, tenantId, UtcNow, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _maintenanceRequestRepositoryMock.Received(1).Add(Arg.Is<MaintenanceRequest>(m =>
            m.ReportedByUserId == tenantId));
    }
}