using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Bookings.ConfirmBooking;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Bookings;

public class ConfirmBookingTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();

    private readonly IBookingRepository _bookingRepositoryMock = Substitute.For<IBookingRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly ConfirmBookingCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public ConfirmBookingTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new ConfirmBookingCommandHandler(
            _bookingRepositoryMock,
            _apartmentRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingNotFound()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        _bookingRepositoryMock.GetByIdAsync(bookingId, Arg.Any<CancellationToken>()).Returns((Booking?)null);

        // Act
        var result = await _handler.Handle(new ConfirmBookingCommand(bookingId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange — an orphaned booking referencing a deleted apartment.
        var apartment = ApartmentData.Create();
        var booking = BookingData.Reserve(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new ConfirmBookingCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange — includes the booking's own guest, who is NOT authorized
        // to confirm their own booking (that's the host's job).
        var apartment = ApartmentData.Create();
        var booking = BookingData.Reserve(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(booking.UserId);
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new ConfirmBookingCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingNotReserved()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveAndConfirm(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new ConfirmBookingCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotReserved);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ConfirmUpdateLastBookedAndSaveChanges_WhenCallerIsOwner()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.Reserve(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new ConfirmBookingCommand(booking.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        apartment.LastBookedOnUtc.Should().Be(UtcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Confirm_WhenCallerIsAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.Reserve(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);

        // Act
        var result = await _handler.Handle(new ConfirmBookingCommand(booking.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }
}