using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.RemoveApartmentAvailabilityBlock;

internal sealed class RemoveApartmentAvailabilityBlockCommandHandler(
    IApartmentAvailabilityBlockRepository blockRepository,
    IApartmentRepository apartmentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveApartmentAvailabilityBlockCommand>
{
    public async Task<Result> Handle(
        RemoveApartmentAvailabilityBlockCommand request,
        CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);

        if (block is null) return Result.Failure(ApartmentAvailabilityBlockErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(block.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure(ApartmentErrors.NotAuthorized);

        blockRepository.Remove(block);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}