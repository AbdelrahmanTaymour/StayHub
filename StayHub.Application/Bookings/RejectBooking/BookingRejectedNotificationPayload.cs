namespace StayHub.Application.Bookings.RejectBooking;

public sealed record BookingRejectedNotificationPayload(
    Guid BookingId,
    Guid ApartmentId,
    string Message);