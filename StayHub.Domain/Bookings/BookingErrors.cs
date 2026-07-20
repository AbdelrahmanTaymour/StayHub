using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Bookings;

public static class BookingErrors
{
    public static Error NotFound = new(
        "Booking.Found",
        "The booking with the specified identifier was not found");

    public static Error Overlap = new(
        "Booking.Overlap",
        "The current booking is overlapping with an existing one");

    public static Error NotReserved = new(
        "Booking.NotReserved",
        "The booking is not pending");

    public static Error NotConfirmed = new(
        "Booking.NotConfirmed",
        "The booking is not confirmed");

    public static Error AlreadyStarted = new(
        "Booking.AlreadyStarted",
        "The booking has already started");

    public static Error AlreadyCheckedIn = new(
        "Booking.AlreadyCheckedIn",
        "The booking has already been checked into");

    public static Error CheckInNotAvailableYet = new(
        "Booking.CheckInNotAvailableYet",
        "Check-in is not available before the booking's start date");

    public static Error NotCheckedIn = new(
        "Booking.NotCheckedIn",
        "The booking has not been checked into yet");

    public static Error AlreadyCheckedOut = new(
        "Booking.AlreadyCheckedOut",
        "The booking has already been checked out of");

    public static Error NotAuthorized = new(
        "Booking.NotAuthorized",
        "Only the guest or the apartment owner can reject this booking");
}