using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Reviews.CreateReview;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Application.UnitTests.Bookings;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Reviews;

namespace StayHub.Application.UnitTests.Reviews;

public class CreateReviewTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IBookingRepository _bookingRepositoryMock = Substitute.For<IBookingRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly CreateReviewCommandHandler _handler;
    private readonly IReviewRepository _reviewRepositoryMock = Substitute.For<IReviewRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public CreateReviewTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new CreateReviewCommandHandler(
            _bookingRepositoryMock,
            _reviewRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static CreateReviewCommand CommandFor(Guid bookingId, int rating = 5) =>
        new(bookingId, rating, "Great stay!");

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingNotFound()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        _bookingRepositoryMock.GetByIdAsync(bookingId, Arg.Any<CancellationToken>()).Returns((Booking?)null);

        // Act
        var result = await _handler.Handle(CommandFor(bookingId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotBookingGuest()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveConfirmAndComplete(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());

        // Act
        var result = await _handler.Handle(CommandFor(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAlreadyReviewed()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveConfirmAndComplete(apartment, guestId);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _reviewRepositoryMock.ExistsForBookingAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(CommandFor(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.AlreadyReviewed);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRatingIsInvalid()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveConfirmAndComplete(apartment, guestId);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _reviewRepositoryMock.ExistsForBookingAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(CommandFor(booking.Id, rating: 6), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Rating.Invalid);
        _reviewRepositoryMock.DidNotReceive().Add(Arg.Any<Review>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingIsNotCompleted()
    {
        // Arrange — surfaces Review.Create's own domain guard (NotEligible)
        // through the handler unchanged.
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveAndConfirm(apartment, guestId); // not yet Completed
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _reviewRepositoryMock.ExistsForBookingAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(CommandFor(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.NotEligible);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateReviewAndSaveChanges_WhenValid()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveConfirmAndComplete(apartment, guestId);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _reviewRepositoryMock.ExistsForBookingAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(CommandFor(booking.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewRepositoryMock.Received(1).Add(Arg.Is<Review>(r =>
            r.Id == result.Value && r.BookingId == booking.Id && r.UserId == guestId));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}