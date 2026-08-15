namespace StayHub.Api.Endpoints.Bookings;

public sealed record ReserveBookingRequest(Guid ApartmentId, DateOnly StartDate, DateOnly EndDate);