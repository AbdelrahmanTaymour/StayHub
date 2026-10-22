using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Apartments.ChangeApartmentStaffRole;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.UnitTests.Apartments;

public class ChangeApartmentStaffRoleCommandHandlerTests
{
    private readonly IApartmentRepository
        _apartmentRepositoryMock =
            Substitute.For<IApartmentRepository>();

    private readonly ChangeApartmentStaffRoleCommandHandler _handler;

    private readonly IApartmentStaffAssignmentRepository
        _staffAssignmentRepositoryMock =
            Substitute.For<IApartmentStaffAssignmentRepository>();

    private readonly IUnitOfWork _unitOfWorkMock =
        Substitute.For<IUnitOfWork>();

    private readonly IUserContext _userContextMock =
        Substitute.For<IUserContext>();

    public ChangeApartmentStaffRoleCommandHandlerTests()
    {
        _handler = new ChangeApartmentStaffRoleCommandHandler(
            _staffAssignmentRepositoryMock,
            _apartmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock);
    }

    private static ChangeApartmentStaffRoleCommand CommandFor(
        Guid assignmentId,
        ApartmentStaffRole role) =>
        new(assignmentId, role);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAssignmentNotFound()
    {
        // Arrange
        var assignmentId = Guid.CreateVersion7();

        _staffAssignmentRepositoryMock
            .GetByIdAsync(
                assignmentId,
                Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        var result = await _handler.Handle(
            CommandFor(assignmentId, ApartmentStaffRole.Manager),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(
            ApartmentStaffAssignmentErrors.NotFound);

        await _apartmentRepositoryMock
            .DidNotReceive()
            .GetByIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await _unitOfWorkMock
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment();

        _staffAssignmentRepositoryMock
            .GetByIdAsync(
                assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(assignment);

        _apartmentRepositoryMock
            .GetByIdAsync(
                assignment.ApartmentId,
                Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(
            CommandFor(assignment.Id, ApartmentStaffRole.Manager),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);

        assignment.Role.Should().NotBe(ApartmentStaffRole.Manager);

        await _unitOfWorkMock
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment();
        var apartment = ApartmentData.Create();

        _staffAssignmentRepositoryMock
            .GetByIdAsync(
                assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(assignment);

        _apartmentRepositoryMock
            .GetByIdAsync(
                assignment.ApartmentId,
                Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(false);

        _userContextMock.IsAdmin.Returns(false);

        // Act
        var result = await _handler.Handle(
            CommandFor(assignment.Id, ApartmentStaffRole.Manager),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);

        await _unitOfWorkMock
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAssignmentIsRevoked()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment(
            role: ApartmentStaffRole.Cleaner);

        assignment.Revoke(DateTime.UtcNow);

        var apartment = ApartmentData.Create();

        _staffAssignmentRepositoryMock
            .GetByIdAsync(
                assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(assignment);

        _apartmentRepositoryMock
            .GetByIdAsync(
                assignment.ApartmentId,
                Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(true);

        // Act
        var result = await _handler.Handle(
            CommandFor(assignment.Id, ApartmentStaffRole.Manager),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(
            ApartmentStaffAssignmentErrors.AlreadyRevoked);

        assignment.Role.Should().Be(ApartmentStaffRole.Cleaner);

        await _unitOfWorkMock
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ChangeRoleAndSaveChanges_WhenValid()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment(
            role: ApartmentStaffRole.Cleaner);

        var apartment = ApartmentData.Create();

        _staffAssignmentRepositoryMock
            .GetByIdAsync(
                assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(assignment);

        _apartmentRepositoryMock
            .GetByIdAsync(
                assignment.ApartmentId,
                Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(true);

        // Act
        var result = await _handler.Handle(
            CommandFor(assignment.Id, ApartmentStaffRole.Manager),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        assignment.Role.Should().Be(ApartmentStaffRole.Manager);

        await _unitOfWorkMock
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenRoleIsUnchanged()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment(
            role: ApartmentStaffRole.Cleaner);

        var apartment = ApartmentData.Create();

        _staffAssignmentRepositoryMock
            .GetByIdAsync(
                assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(assignment);

        _apartmentRepositoryMock
            .GetByIdAsync(
                assignment.ApartmentId,
                Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(true);

        // Act
        var result = await _handler.Handle(
            CommandFor(assignment.Id, ApartmentStaffRole.Cleaner),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        assignment.Role.Should().Be(ApartmentStaffRole.Cleaner);

        await _unitOfWorkMock
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_AllowAdmin_WhenCallerIsNotOwner()
    {
        // Arrange
        var assignment = ApartmentData.CreateStaffAssignment(
            role: ApartmentStaffRole.Cleaner);

        var apartment = ApartmentData.Create();

        _staffAssignmentRepositoryMock
            .GetByIdAsync(
                assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(assignment);

        _apartmentRepositoryMock
            .GetByIdAsync(
                assignment.ApartmentId,
                Arg.Any<CancellationToken>())
            .Returns(apartment);

        _userContextMock
            .IsOwner(apartment.OwnerId)
            .Returns(false);

        _userContextMock.IsAdmin.Returns(true);

        // Act
        var result = await _handler.Handle(
            CommandFor(assignment.Id, ApartmentStaffRole.Manager),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        assignment.Role.Should().Be(ApartmentStaffRole.Manager);

        await _unitOfWorkMock
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}