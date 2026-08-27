using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Payments;
using StayHub.Application.Payments.RefundPayment;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Application.UnitTests.Bookings;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Payments;

public class RefundPaymentTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IBookingRepository _bookingRepositoryMock = Substitute.For<IBookingRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly RefundPaymentCommandHandler _handler;
    private readonly IPaymentGatewayService _paymentGatewayServiceMock = Substitute.For<IPaymentGatewayService>();

    private readonly IPaymentRepository _paymentRepositoryMock = Substitute.For<IPaymentRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public RefundPaymentTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new RefundPaymentCommandHandler(
            _paymentRepositoryMock,
            _bookingRepositoryMock,
            _apartmentRepositoryMock,
            _paymentGatewayServiceMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPaymentNotFound()
    {
        // Arrange
        var paymentId = Guid.CreateVersion7();
        _paymentRepositoryMock.GetByIdAsync(paymentId, Arg.Any<CancellationToken>()).Returns((Payment?)null);

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand(paymentId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenBookingNotFound()
    {
        // Arrange
        var payment = PaymentData.InitiateAndSucceed();
        _paymentRepositoryMock.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bookingRepositoryMock.GetByIdAsync(payment.BookingId, Arg.Any<CancellationToken>()).Returns((Booking?)null);

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand(payment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveAndConfirm(apartment);
        var payment = PaymentData.InitiateAndSucceed(booking.Id);
        _paymentRepositoryMock.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>())
            .Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand(payment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNeitherGuestOwnerNorAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveAndConfirm(apartment);
        var payment = PaymentData.InitiateAndSucceed(booking.Id);
        _paymentRepositoryMock.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand(payment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotAuthorized);
        await _paymentGatewayServiceMock.DidNotReceive()
            .RefundAsync(Arg.Any<ProviderReference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RefundViaGatewayAndSaveChanges_WhenCallerIsGuest()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveAndConfirm(apartment, guestId);
        var payment = PaymentData.InitiateAndSucceed(booking.Id);
        _paymentRepositoryMock.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(guestId);

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand(payment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        await _paymentGatewayServiceMock.Received(1)
            .RefundAsync(payment.ProviderReference, Arg.Any<CancellationToken>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Refund_WhenCallerIsOwner()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveAndConfirm(apartment);
        var payment = PaymentData.InitiateAndSucceed(booking.Id);
        _paymentRepositoryMock.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand(payment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task Handle_Should_Refund_WhenCallerIsAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var booking = BookingData.ReserveAndConfirm(apartment);
        var payment = PaymentData.InitiateAndSucceed(booking.Id);
        _paymentRepositoryMock.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand(payment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPaymentNotSucceeded()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var booking = BookingData.ReserveAndConfirm(apartment, guestId);
        var payment = PaymentData.Initiate(booking.Id); // still Pending
        _paymentRepositoryMock.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _bookingRepositoryMock.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        _apartmentRepositoryMock.GetByIdAsync(booking.ApartmentId, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(guestId);

        // Act
        var result = await _handler.Handle(new RefundPaymentCommand(payment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotSucceeded);
        await _paymentGatewayServiceMock.DidNotReceive()
            .RefundAsync(Arg.Any<ProviderReference>(), Arg.Any<CancellationToken>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}