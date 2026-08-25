using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Bookings.CancelBooking;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Bookings;

public class CancelBookingTests
{
    // Before Duration.Start (2026-01-01) — a valid cancellation time.
    private static readonly DateTime BeforeStart = new(2025, 12, 31);

    // After Duration.Start — triggers the domain's AlreadyStarted guard.
    private static readonly DateTime AfterStart = new(2026, 1, 2);

    private readonly IBookingRepository _bookingRepositoryMock = Substitute.For<IBookingRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly CancelBookingCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public CancelBookingTests()
    {
        _handler = new CancelBookingCommandHandler(
            _bookingRepositoryMock,
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
        var result = await _handler.Handle(new CancelBookingCommand(bookingId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNeitherGuestNorAdmin()
    {
        // Arrange — includes the apartment's OWNER, who is currently NOT
        // authorized to cancel via this handler (see the flag on this).
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveAndConfirm(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _userContextMock.Roles.Returns([]);
        _dateTimeProviderMock.UtcNow.Returns(BeforeStart);

        // Act
        var result = await _handler.Handle(new CancelBookingCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_CancelAndSaveChanges_WhenCallerIsGuest()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveAndConfirm(apartment, guestId);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _dateTimeProviderMock.UtcNow.Returns(BeforeStart);

        // Act
        var result = await _handler.Handle(new CancelBookingCommand(booking.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Cancel_WhenCallerIsAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveAndConfirm(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);
        _dateTimeProviderMock.UtcNow.Returns(BeforeStart);

        // Act
        var result = await _handler.Handle(new CancelBookingCommand(booking.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingAlreadyStarted()
    {
        // Arrange — the domain's own guard, surfaced unchanged through the
        // handler (no re-wrapping into a different error).
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveAndConfirm(apartment, guestId);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _dateTimeProviderMock.UtcNow.Returns(AfterStart);

        // Act
        var result = await _handler.Handle(new CancelBookingCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.AlreadyStarted);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingStatusIsNotCancellable()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.Reserve(apartment, guestId);
        booking.Reject(Guid.CreateVersion7(), DateTime.UtcNow);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _dateTimeProviderMock.UtcNow.Returns(BeforeStart);

        // Act
        var result = await _handler.Handle(new CancelBookingCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotCancellable);
    }
}