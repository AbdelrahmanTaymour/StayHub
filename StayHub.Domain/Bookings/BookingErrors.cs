using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings;

public static class BookingErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Booking.NotFound",
        "The booking with the specified identifier was not found");

    public static readonly Error Overlap = Error.Conflict(
        "Booking.Overlap",
        "The current booking is overlapping with an existing one");

    public static readonly Error NotReserved = Error.Conflict(
        "Booking.NotReserved",
        "The booking is not pending");

    public static readonly Error NotConfirmed = Error.Conflict(
        "Booking.NotConfirmed",
        "The booking is not confirmed");

    public static readonly Error AlreadyStarted = Error.Conflict(
        "Booking.AlreadyStarted",
        "The booking has already started");

    public static readonly Error NotCancellable = Error.Conflict(
        "Booking.NotCancellable",
        "The booking cannot be cancelled in its current status");

    public static readonly Error NotAuthorized = Error.Forbidden(
        "Booking.NotAuthorized",
        "You're not authorized to perform this action");

    public static readonly Error InvalidDuration = Error.Validation(
        "Booking.InvalidDuration",
        "The booking duration must be at least 1 night");
}