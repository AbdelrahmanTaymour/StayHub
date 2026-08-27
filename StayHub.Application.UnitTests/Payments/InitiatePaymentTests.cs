using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Payments;
using StayHub.Application.Payments.InitiatePayment;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Application.UnitTests.Bookings;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;

namespace StayHub.Application.UnitTests.Payments;

public class InitiatePaymentTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private static readonly PaymentIntentResult Intent = new("pi_new_123", "cs_test_secret");

    private readonly IBookingRepository _bookingRepositoryMock = Substitute.For<IBookingRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly InitiatePaymentCommandHandler _handler;
    private readonly IPaymentGatewayService _paymentGatewayServiceMock = Substitute.For<IPaymentGatewayService>();
    private readonly IPaymentRepository _paymentRepositoryMock = Substitute.For<IPaymentRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public InitiatePaymentTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);
        _paymentGatewayServiceMock
            .CreatePaymentIntentAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Intent);

        _handler = new InitiatePaymentCommandHandler(
            _bookingRepositoryMock,
            _paymentRepositoryMock,
            _paymentGatewayServiceMock,
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
        var result = await _handler.Handle(new InitiatePaymentCommand(bookingId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotBookingGuest()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveAndConfirm(apartment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());

        // Act
        var result = await _handler.Handle(new InitiatePaymentCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotAuthorized);
        await _paymentGatewayServiceMock.DidNotReceive().CreatePaymentIntentAsync(
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingNotConfirmed()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.Reserve(apartment, guestId); // still Reserved
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);

        // Act
        var result = await _handler.Handle(new InitiatePaymentCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.BookingNotConfirmed);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenActivePaymentAlreadyExists()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveAndConfirm(apartment, guestId);
        var existingPayment = PaymentData.Initiate(booking.Id);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _paymentRepositoryMock.GetActiveByBookingIdAsync(booking.Id, Arg.Any<CancellationToken>())
            .Returns(existingPayment);

        // Act
        var result = await _handler.Handle(new InitiatePaymentCommand(booking.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.AlreadyInitiated);
        await _paymentGatewayServiceMock.DidNotReceive().CreatePaymentIntentAsync(
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreatePaymentAndSaveChanges_WhenValid()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveAndConfirm(apartment, guestId);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _paymentRepositoryMock.GetActiveByBookingIdAsync(booking.Id, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);

        // Act
        var result = await _handler.Handle(new InitiatePaymentCommand(booking.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentId.Should().NotBeEmpty();
        result.Value.ClientSecret.Should().Be(Intent.ClientSecret);
        _paymentRepositoryMock.Received(1).Add(Arg.Is<Payment>(p =>
            p.BookingId == booking.Id && p.ProviderReference.Value == Intent.ProviderReference));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // This test ensures if the local commit fails after the
    // PaymentIntent was already created on payment gateway side,
    // refund it rather than leave an orphan.
    [Fact]
    public async Task Handle_Should_CancelPaymentIntentAndRethrow_WhenSaveChangesFails()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveAndConfirm(apartment, guestId);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _userContextMock.UserId.Returns(guestId);
        _paymentRepositoryMock.GetActiveByBookingIdAsync(booking.Id, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        _unitOfWorkMock.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        // Act
        var act = () => _handler.Handle(new InitiatePaymentCommand(booking.Id), default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _paymentGatewayServiceMock.Received(1)
            .RefundAsync(Intent.ProviderReference, Arg.Any<CancellationToken>());
    }
}