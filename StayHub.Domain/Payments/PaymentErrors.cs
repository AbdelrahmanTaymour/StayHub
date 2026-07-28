using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Payments;

public static class PaymentErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Payment.NotFound",
        "The payment with the specified identifier was not found");

    public static readonly Error NotPending = Error.Conflict(
        "Payment.NotPending",
        "The payment is not pending");

    public static readonly Error NotSucceeded = Error.Conflict(
        "Payment.NotSucceeded",
        "The payment has not succeeded");

    public static readonly Error NotAuthorized = Error.Unauthorized(
        "Payment.NotAuthorized",
        "Only the guest who made this booking can pay for it");

    public static readonly Error BookingNotConfirmed = Error.Conflict(
        "Payment.BookingNotConfirmed",
        "A booking must be confirmed before it can be paid for");

    public static readonly Error AlreadyInitiated = Error.Conflict(
        "Payment.AlreadyInitiated",
        "A payment has already been initiated for this booking");
}