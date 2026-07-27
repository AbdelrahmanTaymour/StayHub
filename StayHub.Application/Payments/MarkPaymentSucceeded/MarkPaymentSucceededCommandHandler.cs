using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Payments;

namespace StayHub.Application.Payments.MarkPaymentSucceeded;

internal sealed class MarkPaymentSucceededCommandHandler(
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MarkPaymentSucceededCommand>
{
    public async Task<Result> Handle(MarkPaymentSucceededCommand request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByProviderReferenceAsync(request.ProviderReference, cancellationToken);

        if (payment is null) return Result.Failure(PaymentErrors.NotFound);

        var result = payment.MarkAsSucceeded(dateTimeProvider.UtcNow);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}