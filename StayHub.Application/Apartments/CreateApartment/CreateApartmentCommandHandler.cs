using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Shared;

namespace StayHub.Application.Apartments.CreateApartment;

internal sealed class CreateApartmentCommandHandler(
    IApartmentRepository apartmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateApartmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateApartmentCommand request, CancellationToken cancellationToken)
    {
        var address = Address.Create(request.Street, request.City, request.State, request.ZipCode, request.Country);
        var price = new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency));
        var cleaningFee = new Money(request.CleaningFeeAmount, Currency.FromCode(request.CleaningFeeCurrency));

        var apartment = Apartment.Create(
            userContext.UserId, // ownerId
            new Name(request.Name),
            new Description(request.Description),
            address,
            price,
            cleaningFee,
            dateTimeProvider.UtcNow);

        apartmentRepository.Add(apartment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return apartment.Id;
    }
}