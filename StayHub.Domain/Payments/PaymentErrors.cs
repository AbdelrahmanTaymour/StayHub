using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments;

public static class PaymentErrors
{
    public static Error NotFound = new(
        "Payment.NotFound",
        "The payment with the specified identifier was not found");

    public static Error NotPending = new(
        "Payment.NotPending",
        "The payment is not pending");

    public static Error NotSucceeded = new(
        "Payment.NotSucceeded",
        "The payment has not succeeded");
}