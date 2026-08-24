using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Apartments.RevokeApartmentStaffAssignment;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.UnitTests.Apartments;

public class RevokeApartmentStaffAssignmentCommandHandlerTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly RevokeApartmentStaffAssignmentCommandHandler _handler;

    private readonly IApartmentStaffAssignmentRepository _staffAssignmentRepositoryMock =
        Substitute.For<IApartmentStaffAssignmentRepository>();

    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public RevokeApartmentStaffAssignmentCommandHandlerTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new RevokeApartmentStaffAssignmentCommandHandler(
            _staffAssignmentRepositoryMock,
            _apartmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static ApartmentStaffAssignment CreateAssignment(Guid apartmentId) =>
        ApartmentStaffAssignment.Create(apartmentId, Guid.CreateVersion7(), ApartmentStaffRole.Cleaner, UtcNow);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAssignmentNotFound()
    {
        // Arrange
        var assignmentId = Guid.CreateVersion7();
        _staffAssignmentRepositoryMock.GetByIdAsync(assignmentId, Arg.Any<CancellationToken>())
            .Returns((ApartmentStaffAssignment?)null);

        // Act
        var result = await _handler.Handle(new RevokeApartmentStaffAssignmentCommand(assignmentId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentStaffAssignmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var assignment = CreateAssignment(Guid.CreateVersion7());
        _staffAssignmentRepositoryMock.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        _apartmentRepositoryMock.GetByIdAsync(assignment.ApartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new RevokeApartmentStaffAssignmentCommand(assignment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange — note the assigned staff member themselves is NOT
        // authorized to revoke their own assignment; only the apartment
        // owner or an admin can. It's a deliberate design choice.
        var apartment = ApartmentData.Create();
        var assignment = CreateAssignment(apartment.Id);
        _staffAssignmentRepositoryMock.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(assignment.UserId);
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new RevokeApartmentStaffAssignmentCommand(assignment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAlreadyRevoked()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var assignment = CreateAssignment(apartment.Id);
        assignment.Revoke(UtcNow);
        _staffAssignmentRepositoryMock.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new RevokeApartmentStaffAssignmentCommand(assignment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentStaffAssignmentErrors.AlreadyRevoked);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RevokeAndSaveChanges_WhenCallerOwnsApartment()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var assignment = CreateAssignment(apartment.Id);
        _staffAssignmentRepositoryMock.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new RevokeApartmentStaffAssignmentCommand(assignment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        assignment.RevokedOnUtc.Should().Be(UtcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}