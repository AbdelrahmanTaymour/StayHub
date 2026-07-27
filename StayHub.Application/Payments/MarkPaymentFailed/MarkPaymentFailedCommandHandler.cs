using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Payments;

namespace StayHub.Application.Payments.MarkPaymentFailed;

internal sealed class MarkPaymentFailedCommandHandler(
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<MarkPaymentFailedCommand>
{
    public async Task<Result> Handle(MarkPaymentFailedCommand request, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByProviderReferenceAsync(request.ProviderReference, cancellationToken);

        if (payment is null) return Result.Failure(PaymentErrors.NotFound);

        var result = payment.MarkAsFailed(DateTime.UtcNow);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}