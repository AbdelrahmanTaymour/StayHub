using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Apartments.AssignApartmentStaff;
using StayHub.Application.UnitTests.Users;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Apartments;

public class AssignApartmentStaffCommandHandlerTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly AssignApartmentStaffCommandHandler _handler;

    private readonly IApartmentStaffAssignmentRepository _staffAssignmentRepositoryMock =
        Substitute.For<IApartmentStaffAssignmentRepository>();

    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();
    private readonly IUserRepository _userRepositoryMock = Substitute.For<IUserRepository>();

    public AssignApartmentStaffCommandHandlerTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new AssignApartmentStaffCommandHandler(
            _apartmentRepositoryMock,
            _userRepositoryMock,
            _staffAssignmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static AssignApartmentStaffCommand CommandFor(Guid apartmentId, Guid staffUserId) =>
        new(apartmentId, staffUserId, ApartmentStaffRole.Cleaner);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(CommandFor(apartmentId, Guid.CreateVersion7()), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id, Guid.CreateVersion7()), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenStaffUserNotFound()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var staffUserId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _userRepositoryMock.GetByIdAsync(staffUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id, staffUserId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenActiveAssignmentAlreadyExists()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var staffUser = UserData.Create();
        var existingAssignment = ApartmentStaffAssignment.Create(
            apartment.Id, staffUser.Id, ApartmentStaffRole.Manager, UtcNow);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _userRepositoryMock.GetByIdAsync(staffUser.Id, Arg.Any<CancellationToken>()).Returns(staffUser);
        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, staffUser.Id, Arg.Any<CancellationToken>())
            .Returns(existingAssignment);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id, staffUser.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentStaffAssignmentErrors.AlreadyAssigned);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateAssignmentAndSaveChanges_WhenValid()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var staffUser = UserData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _userRepositoryMock.GetByIdAsync(staffUser.Id, Arg.Any<CancellationToken>()).Returns(staffUser);
        _staffAssignmentRepositoryMock
            .GetActiveAsync(apartment.Id, staffUser.Id, Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id, staffUser.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _staffAssignmentRepositoryMock.Received(1).Add(Arg.Is<ApartmentStaffAssignment>(a =>
            a.Id == result.Value && a.ApartmentId == apartment.Id && a.UserId == staffUser.Id));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}