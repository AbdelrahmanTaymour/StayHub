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

    public static Error NotAuthorized = new(
        "Payment.NotAuthorized",
        "Only the guest who made this booking can pay for it");

    public static Error BookingNotConfirmed = new(
        "Payment.BookingNotConfirmed",
        "A booking must be confirmed before it can be paid for");

    public static Error AlreadyInitiated = new(
        "Payment.AlreadyInitiated",
        "A payment has already been initiated for this booking");
}