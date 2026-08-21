using FluentAssertions;
using StayHub.Domain.Payments;
using StayHub.Domain.Payments.Events;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Payments;

public class PaymentTests : BaseTest
{
    [Fact]
    public void Initiate_Should_SetPropertyValues()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();

        // Act
        var payment = Payment.Initiate(
            bookingId,
            PaymentData.Amount,
            PaymentProvider.Stripe,
            PaymentData.ProviderReference,
            DateTime.UtcNow);

        // Assert
        payment.BookingId.Should().Be(bookingId);
        payment.Amount.Should().Be(PaymentData.Amount);
        payment.Provider.Should().Be(PaymentProvider.Stripe);
        payment.ProviderReference.Should().Be(PaymentData.ProviderReference);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.ProcessedOnUtc.Should().BeNull();
    }

    [Fact]
    public void Initiate_Should_RaisePaymentInitiatedDomainEvent()
    {
        // Act
        var payment = PaymentData.Initiate();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<PaymentInitiatedDomainEvent>(payment);
        domainEvent.PaymentId.Should().Be(payment.Id);
        domainEvent.BookingId.Should().Be(payment.BookingId);
    }

    [Fact]
    public void MarkAsSucceeded_Should_SetStatusSucceededAndReturnSuccess_WhenPending()
    {
        // Arrange
        var payment = PaymentData.Initiate();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = payment.MarkAsSucceeded(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.ProcessedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void MarkAsSucceeded_Should_RaisePaymentSucceededDomainEvent_WhenPending()
    {
        // Arrange
        var payment = PaymentData.Initiate();

        // Act
        payment.MarkAsSucceeded(DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<PaymentSucceededDomainEvent>(payment);
        domainEvent.PaymentId.Should().Be(payment.Id);
        domainEvent.BookingId.Should().Be(payment.BookingId);
    }

    [Fact]
    public void MarkAsSucceeded_Should_ReturnFailure_WhenAlreadySucceeded()
    {
        // Arrange
        var payment = PaymentData.InitiateAndSucceed();

        // Act
        var result = payment.MarkAsSucceeded(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotPending);
    }

    [Fact]
    public void MarkAsFailed_Should_SetStatusFailedAndReturnSuccess_WhenPending()
    {
        // Arrange
        var payment = PaymentData.Initiate();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = payment.MarkAsFailed(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.ProcessedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void MarkAsFailed_Should_RaisePaymentFailedDomainEvent_WhenPending()
    {
        // Arrange
        var payment = PaymentData.Initiate();

        // Act
        payment.MarkAsFailed(DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<PaymentFailedDomainEvent>(payment);
        domainEvent.PaymentId.Should().Be(payment.Id);
        domainEvent.BookingId.Should().Be(payment.BookingId);
    }

    [Fact]
    public void MarkAsFailed_Should_ReturnFailure_WhenAlreadySucceeded()
    {
        // Arrange
        var payment = PaymentData.InitiateAndSucceed();

        // Act
        var result = payment.MarkAsFailed(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotPending);
    }

    [Fact]
    public void Refund_Should_SetStatusRefundedAndReturnSuccess_WhenSucceeded()
    {
        // Arrange
        var payment = PaymentData.InitiateAndSucceed();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = payment.Refund(utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.ProcessedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Refund_Should_RaisePaymentRefundedDomainEvent_WhenSucceeded()
    {
        // Arrange
        var payment = PaymentData.InitiateAndSucceed();

        // Act
        payment.Refund(DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<PaymentRefundedDomainEvent>(payment);
        domainEvent.PaymentId.Should().Be(payment.Id);
        domainEvent.BookingId.Should().Be(payment.BookingId);
    }

    [Fact]
    public void Refund_Should_ReturnFailure_WhenStillPending()
    {
        // Arrange — can't refund a payment that never actually succeeded.
        var payment = PaymentData.Initiate();

        // Act
        var result = payment.Refund(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotSucceeded);
    }

    [Fact]
    public void Refund_Should_ReturnFailure_WhenPaymentFailed()
    {
        // Arrange — a genuinely different terminal state than "still
        // pending," worth confirming the same guard catches it too.
        var payment = PaymentData.InitiateAndFail();

        // Act
        var result = payment.Refund(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotSucceeded);
    }
}