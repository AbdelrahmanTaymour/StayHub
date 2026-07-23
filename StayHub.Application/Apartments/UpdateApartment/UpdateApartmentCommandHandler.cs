using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Shared;

namespace StayHub.Application.Apartments.UpdateApartment;

internal sealed class UpdateApartmentCommandHandler(
    IApartmentRepository apartmentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateApartmentCommand>
{
    public async Task<Result> Handle(UpdateApartmentCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure(ApartmentErrors.NotAuthorized);

        var price = new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency));
        var cleaningFee = new Money(request.CleaningFeeAmount, Currency.FromCode(request.CleaningFeeCurrency));

        apartment.UpdateDetails(new Name(request.Name), new Description(request.Description), price, cleaningFee);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}