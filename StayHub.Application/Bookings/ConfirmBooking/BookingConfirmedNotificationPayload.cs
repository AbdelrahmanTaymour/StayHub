namespace StayHub.Application.Bookings.ConfirmBooking;

public sealed record BookingConfirmedNotificationPayload(
    Guid BookingId,
    string Message);