using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Payments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;

namespace StayHub.Application.Payments.RefundPayment;

internal sealed class RefundPaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IPaymentGatewayService paymentGatewayService,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RefundPaymentCommand>
{
    public async Task<Result> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);

        if (payment is null) return Result.Failure(PaymentErrors.NotFound);

        var booking = await bookingRepository.GetByIdAsync(payment.BookingId, cancellationToken);

        if (booking is null) return Result.Failure(BookingErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        var isGuest = booking.UserId == userContext.UserId;
        var isOwner = apartment.OwnerId == userContext.UserId;

        if (!isGuest && !isOwner) return Result.Failure(PaymentErrors.NotAuthorized);

        await paymentGatewayService.RefundAsync(payment.ProviderReference, cancellationToken);

        var result = payment.Refund(dateTimeProvider.UtcNow);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}