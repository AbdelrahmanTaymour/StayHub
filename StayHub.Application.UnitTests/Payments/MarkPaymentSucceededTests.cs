using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Payments.MarkPaymentSucceeded;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Payments;

namespace StayHub.Application.UnitTests.Payments;

public class MarkPaymentSucceededTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly MarkPaymentSucceededCommandHandler _handler;

    private readonly IPaymentRepository _paymentRepositoryMock = Substitute.For<IPaymentRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();

    public MarkPaymentSucceededTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new MarkPaymentSucceededCommandHandler(_paymentRepositoryMock, _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPaymentNotFound()
    {
        // Arrange
        var reference = new ProviderReference("pi_unknown");
        _paymentRepositoryMock.GetByProviderReferenceAsync(reference, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);

        // Act
        var result = await _handler.Handle(new MarkPaymentSucceededCommand(reference), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_MarkAsSucceededAndSaveChanges_WhenPending()
    {
        // Arrange
        var payment = PaymentData.Initiate();
        _paymentRepositoryMock.GetByProviderReferenceAsync(payment.ProviderReference, Arg.Any<CancellationToken>())
            .Returns(payment);

        // Act
        var result = await _handler.Handle(new MarkPaymentSucceededCommand(payment.ProviderReference), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.ProcessedOnUtc.Should().Be(UtcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPaymentNotPending()
    {
        // Arrange
        var payment = PaymentData.InitiateAndSucceed();
        _paymentRepositoryMock.GetByProviderReferenceAsync(payment.ProviderReference, Arg.Any<CancellationToken>())
            .Returns(payment);

        // Act
        var result = await _handler.Handle(new MarkPaymentSucceededCommand(payment.ProviderReference), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotPending);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}