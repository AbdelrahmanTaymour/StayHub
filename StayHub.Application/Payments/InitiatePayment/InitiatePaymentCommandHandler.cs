using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Payments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;
using StayHub.Domain.Shared;

namespace StayHub.Application.Payments.InitiatePayment;

internal sealed class InitiatePaymentCommandHandler(
    IBookingRepository bookingRepository,
    IPaymentRepository paymentRepository,
    IPaymentGatewayService paymentGatewayService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<InitiatePaymentCommand, InitiatePaymentResponse>
{
    public async Task<Result<InitiatePaymentResponse>> Handle(
        InitiatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null) return Result.Failure<InitiatePaymentResponse>(BookingErrors.NotFound);

        if (booking.UserId != request.RequestedByUserId)
            return Result.Failure<InitiatePaymentResponse>(PaymentErrors.NotAuthorized);

        if (booking.Status != BookingStatus.Confirmed)
            return Result.Failure<InitiatePaymentResponse>(PaymentErrors.BookingNotConfirmed);

        var existingPayment = await paymentRepository.GetByBookingIdAsync(booking.Id, cancellationToken);

        if (existingPayment is not null) return Result.Failure<InitiatePaymentResponse>(PaymentErrors.AlreadyInitiated);

        var intent = await paymentGatewayService.CreatePaymentIntentAsync(
            booking.TotalPrice.Amount,
            booking.TotalPrice.Currency.Code,
            cancellationToken);

        var amount = new Money(booking.TotalPrice.Amount, booking.TotalPrice.Currency);

        var payment = Payment.Initiate(
            booking.Id,
            amount,
            PaymentProvider.Stripe,
            new ProviderReference(intent.ProviderReference),
            dateTimeProvider.UtcNow);

        paymentRepository.Add(payment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InitiatePaymentResponse(payment.Id, intent.ClientSecret);
    }
}