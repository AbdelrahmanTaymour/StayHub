using StayHub.Domain.Abstractions;
using StayHub.Domain.Payments.Events;
using StayHub.Domain.Shared;

namespace StayHub.Domain.Payments;

public sealed class Payment : Entity
{
    private Payment(Guid id,
        Guid bookingId,
        Money amount,
        PaymentProvider provider,
        PaymentStatus status,
        DateTime createdOnUtc) : base(id)
    {
        BookingId = bookingId;
        Amount = amount;
        Provider = provider;
        Status = status;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid BookingId { get; }
    public Money Amount { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public ProviderReference ProviderReference { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }

    public static Payment Initiate(
        Guid bookingId,
        Money amount,
        PaymentProvider provider,
        DateTime utcNow)
    {
        var payment = new Payment(
            Guid.CreateVersion7(),
            bookingId,
            amount,
            provider,
            PaymentStatus.Pending,
            utcNow);

        payment.RaiseDomainEvent(new PaymentInitiatedDomainEvent(payment.Id, payment.BookingId));

        return payment;
    }

    public Result MarkAsSucceeded(ProviderReference providerReference, DateTime utcNow)
    {
        if (Status != PaymentStatus.Pending) return Result.Failure(PaymentErrors.NotPending);

        Status = PaymentStatus.Succeeded;
        ProviderReference = providerReference;
        ProcessedOnUtc = utcNow;

        RaiseDomainEvent(new PaymentSucceededDomainEvent(Id, BookingId));

        return Result.Success();
    }

    public Result MarkAsFailed(DateTime utcNow)
    {
        if (Status != PaymentStatus.Pending) return Result.Failure(PaymentErrors.NotPending);

        Status = PaymentStatus.Failed;
        ProcessedOnUtc = utcNow;

        RaiseDomainEvent(new PaymentFailedDomainEvent(Id, BookingId));

        return Result.Success();
    }

    public Result Refund(DateTime utcNow)
    {
        if (Status != PaymentStatus.Succeeded) return Result.Failure(PaymentErrors.NotSucceeded);

        Status = PaymentStatus.Refunded;
        ProcessedOnUtc = utcNow;

        RaiseDomainEvent(new PaymentRefundedDomainEvent(Id, BookingId));

        return Result.Success();
    }
}