namespace StayHub.Application.Bookings.RejectBooking;

public sealed record BookingRejectedNotificationPayload(
    Guid BookingId,
    string Message);