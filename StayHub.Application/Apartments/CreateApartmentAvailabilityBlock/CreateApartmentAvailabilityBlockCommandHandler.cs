using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Apartments.CreateApartmentAvailabilityBlock;

internal sealed class CreateApartmentAvailabilityBlockCommandHandler(
    IApartmentRepository apartmentRepository,
    IApartmentAvailabilityBlockRepository blockRepository,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateApartmentAvailabilityBlockCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateApartmentAvailabilityBlockCommand request,
        CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure<Guid>(ApartmentErrors.NotAuthorized);

        var blockOverlap = await blockRepository.IsOverlappingAsync(
            request.ApartmentId,
            request.Start,
            request.End,
            cancellationToken);

        if (blockOverlap) return Result.Failure<Guid>(ApartmentAvailabilityBlockErrors.Overlap);

        var duration = DateRange.Create(request.Start, request.End);

        var bookingOverlap = await bookingRepository.IsOverlappingAsync(apartment, duration, cancellationToken);

        if (bookingOverlap) return Result.Failure<Guid>(ApartmentAvailabilityBlockErrors.Overlap);

        var block = ApartmentAvailabilityBlock.Create(request.ApartmentId, request.Start, request.End, request.Reason);

        blockRepository.Add(block);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return block.Id;
    }
}