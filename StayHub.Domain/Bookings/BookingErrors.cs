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

    public static readonly Error AlreadyCheckedIn = Error.Conflict(
        "Booking.AlreadyCheckedIn",
        "The booking has already been checked into");

    public static readonly Error CheckInNotAvailableYet = Error.Conflict(
        "Booking.CheckInNotAvailableYet",
        "Check-in is not available before the booking's start date");

    public static readonly Error NotCheckedIn = Error.Conflict(
        "Booking.NotCheckedIn",
        "The booking has not been checked into yet");

    public static readonly Error AlreadyCheckedOut = Error.Conflict(
        "Booking.AlreadyCheckedOut",
        "The booking has already been checked out of");

    public static readonly Error NotAuthorized = Error.Unauthorized(
        "Booking.NotAuthorized",
        "Only the guest or the apartment owner can reject this booking");
}