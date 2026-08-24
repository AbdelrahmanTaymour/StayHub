using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Apartments.CreateApartmentAvailabilityBlock;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.UnitTests.Apartments;

public class CreateApartmentAvailabilityBlockCommandHandlerTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private static readonly DateOnly Start = new(2026, 3, 1);
    private static readonly DateOnly End = new(2026, 3, 10);

    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly IApartmentAvailabilityBlockRepository _blockRepositoryMock =
        Substitute.For<IApartmentAvailabilityBlockRepository>();

    private readonly IBookingRepository _bookingRepositoryMock = Substitute.For<IBookingRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly CreateApartmentAvailabilityBlockCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public CreateApartmentAvailabilityBlockCommandHandlerTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new CreateApartmentAvailabilityBlockCommandHandler(
            _apartmentRepositoryMock,
            _blockRepositoryMock,
            _bookingRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static CreateApartmentAvailabilityBlockCommand CommandFor(Guid apartmentId) =>
        new(apartmentId, Start, End, ApartmentUnavailabilityReason.OwnerBlocked);

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
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenOverlappingExistingBlock()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _blockRepositoryMock.IsOverlappingAsync(apartment.Id, Start, End, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentAvailabilityBlockErrors.Overlap);
        await _bookingRepositoryMock.DidNotReceive()
            .IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenOverlappingExistingBooking()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _blockRepositoryMock.IsOverlappingAsync(apartment.Id, Start, End, Arg.Any<CancellationToken>()).Returns(false);
        _bookingRepositoryMock
            .IsOverlappingAsync(apartment, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentAvailabilityBlockErrors.Overlap);
    }

    [Fact]
    public async Task Handle_Should_CreateBlockAndSaveChanges_WhenNoOverlap()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _blockRepositoryMock.IsOverlappingAsync(apartment.Id, Start, End, Arg.Any<CancellationToken>()).Returns(false);
        _bookingRepositoryMock
            .IsOverlappingAsync(apartment, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _blockRepositoryMock.Received(1).Add(Arg.Is<ApartmentAvailabilityBlock>(b =>
            b.Id == result.Value && b.Start == Start && b.End == End));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_WhenStartIsAfterEnd()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _blockRepositoryMock
            .IsOverlappingAsync(apartment.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var command = new CreateApartmentAvailabilityBlockCommand(
            apartment.Id, End, Start, ApartmentUnavailabilityReason.OwnerBlocked); // swapped

        // Act
        var act = () => _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ApplicationException>();
    }
}