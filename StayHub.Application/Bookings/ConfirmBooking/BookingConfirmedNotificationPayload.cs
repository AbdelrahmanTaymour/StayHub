namespace StayHub.Application.Bookings.ConfirmBooking;

public sealed record BookingConfirmedNotificationPayload(
    Guid BookingId,
    Guid ApartmentId,
    string Message);