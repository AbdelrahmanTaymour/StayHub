using StayHub.Domain.Apartments;
using StayHub.Domain.Shared;

namespace StayHub.Domain.Bookings;

public class PricingService
{
    public PricingDetails CalculatePrice(Apartment apartment, DateRange period)
    {
        return CalculatePrice(apartment.Price, apartment.CleaningFee, apartment.Amenities, period.LengthInDays);
    }

    public PricingDetails CalculatePrice(
        Money price,
        Money cleaningFee,
        IEnumerable<Amenity> amenities,
        int lengthInDays)
    {
        var currency = price.Currency;

        var priceForPeriod = new Money(price.Amount * lengthInDays, currency);

        decimal percentageUpCharge = 0;
        foreach (var amenity in amenities)
            percentageUpCharge += amenity switch
            {
                Amenity.GardenView or Amenity.MountainView => 0.05m,
                Amenity.AirConditioning => 0.01m,
                Amenity.Parking => 0.01m,
                _ => 0.01m
            };

        var amenitiesUpCharge = Money.Zero(currency);
        if (percentageUpCharge > 0) amenitiesUpCharge = new Money(priceForPeriod.Amount * percentageUpCharge, currency);

        var totalPrice = Money.Zero(currency);
        totalPrice += priceForPeriod;

        if (!cleaningFee.IsZero()) totalPrice += cleaningFee;

        totalPrice += amenitiesUpCharge;

        return new PricingDetails(priceForPeriod, cleaningFee.Copy(), amenitiesUpCharge, totalPrice);
    }
}