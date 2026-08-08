namespace StayHub.Api.Controllers.Bookings;

public sealed record ReserveBookingRequest(Guid ApartmentId, DateOnly StartDate, DateOnly EndDate);