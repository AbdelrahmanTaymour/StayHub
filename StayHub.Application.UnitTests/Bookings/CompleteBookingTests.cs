using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Bookings.CompleteBooking;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;

namespace StayHub.Application.UnitTests.Bookings;

public class CompleteBookingTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IBookingRepository _bookingRepositoryMock = Substitute.For<IBookingRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly CompleteBookingCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();

    public CompleteBookingTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new CompleteBookingCommandHandler(_bookingRepositoryMock, _unitOfWorkMock, _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingNotFound()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        _bookingRepositoryMock.GetByIdAsync(bookingId, Arg.Any<CancellationToken>()).Returns((Booking?)null);

        // Act
        var result = await _handler.Handle(new CompleteBookingCommand(bookingId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingNotConfirmed()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.Reserve(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        // Act
        var result = await _handler.Handle(new CompleteBookingCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotConfirmed);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CompleteAndSaveChanges_WhenConfirmed()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveAndConfirm(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        // Act
        var result = await _handler.Handle(new CompleteBookingCommand(booking.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Completed);
        booking.CompletedOnUtc.Should().Be(UtcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}