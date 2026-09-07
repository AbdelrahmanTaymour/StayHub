using FluentValidation;

namespace StayHub.Application.Bookings.ReserveBooking;

internal sealed class ReserveBookingCommandValidator : AbstractValidator<ReserveBookingCommand>
{
    public ReserveBookingCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();

        RuleFor(c => c.StartDate).LessThan(c => c.EndDate);
    }
}