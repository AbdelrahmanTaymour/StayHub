namespace StayHub.Application.Bookings.CancelBooking;

public sealed record BookingCancelledNotificationPayload(Guid BookingId, string Message);