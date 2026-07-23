using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Shared;

namespace StayHub.Application.Apartments.CreateApartment;

internal sealed class CreateApartmentCommandHandler(
    IApartmentRepository apartmentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateApartmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateApartmentCommand request, CancellationToken cancellationToken)
    {
        var address = Address.Create(request.Street, request.City, request.State, request.ZipCode, request.Country);

        var price = new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency));
        var cleaningFee = new Money(request.CleaningFeeAmount, Currency.FromCode(request.CleaningFeeCurrency));

        var apartment = Apartment.Create(
            request.OwnerId,
            new Name(request.Name),
            new Description(request.Description),
            address,
            price,
            cleaningFee,
            DateTime.UtcNow);

        apartmentRepository.Add(apartment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return apartment.Id;
    }
}