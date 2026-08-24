using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Apartments.RemoveApartmentAvailabilityBlock;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.UnitTests.Apartments;

public class RemoveApartmentAvailabilityBlockCommandHandlerTests
{
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly IApartmentAvailabilityBlockRepository _blockRepositoryMock =
        Substitute.For<IApartmentAvailabilityBlockRepository>();

    private readonly RemoveApartmentAvailabilityBlockCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public RemoveApartmentAvailabilityBlockCommandHandlerTests()
    {
        _handler = new RemoveApartmentAvailabilityBlockCommandHandler(
            _blockRepositoryMock,
            _apartmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock);
    }

    private static ApartmentAvailabilityBlock CreateBlock(Guid apartmentId) =>
        ApartmentAvailabilityBlock.Create(
            apartmentId,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 10),
            ApartmentUnavailabilityReason.OwnerBlocked,
            DateTime.UtcNow);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBlockNotFound()
    {
        // Arrange
        var blockId = Guid.CreateVersion7();
        _blockRepositoryMock.GetByIdAsync(blockId, Arg.Any<CancellationToken>())
            .Returns((ApartmentAvailabilityBlock?)null);

        // Act
        var result = await _handler.Handle(new RemoveApartmentAvailabilityBlockCommand(blockId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentAvailabilityBlockErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var block = CreateBlock(Guid.CreateVersion7());
        _blockRepositoryMock.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        _apartmentRepositoryMock.GetByIdAsync(block.ApartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new RemoveApartmentAvailabilityBlockCommand(block.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var block = CreateBlock(apartment.Id);
        _blockRepositoryMock.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new RemoveApartmentAvailabilityBlockCommand(block.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
        _blockRepositoryMock.DidNotReceive().Remove(Arg.Any<ApartmentAvailabilityBlock>());
    }

    [Fact]
    public async Task Handle_Should_RemoveBlockAndSaveChanges_WhenCallerOwnsApartment()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var block = CreateBlock(apartment.Id);
        _blockRepositoryMock.GetByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new RemoveApartmentAvailabilityBlockCommand(block.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _blockRepositoryMock.Received(1).Remove(block);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}