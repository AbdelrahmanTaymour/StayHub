using FluentAssertions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Shared;
using StayHub.Domain.UnitTests.Apartments;

namespace StayHub.Domain.UnitTests.Bookings;

public class PricingServiceTest
{
    [Fact]
    public void CalculatePrice_Should_ReturnCorrectTotalPrice()
    {
        // Arrange
        var price = new Money(10m, Currency.Usd);
        var cleaningFee = new Money(20m, Currency.Usd);
        var period = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 10, 1));
        var apartment = ApartmentData.Create(price, cleaningFee);
        var pricingService = new PricingService();
        var expectedTotalPrice = new Money(
            price.Amount * period.LengthInDays + cleaningFee.Amount, Currency.Usd);

        // Act
        var pricingDetails = pricingService.CalculatePrice(apartment, period);

        // Assert
        pricingDetails.TotalPrice.Should().Be(expectedTotalPrice);
    }

    [Fact]
    public void CalculatePrice_Should_ReturnCorrectTotalPrice_WhenCleaningFeeIsIncluded()
    {
        // Arrange
        var price = new Money(10.0m, Currency.Usd);
        var cleaningFee = new Money(99.99m, Currency.Usd);
        var period = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 10, 1));
        var expectedTotalPrice = new Money(price.Amount * period.LengthInDays + cleaningFee.Amount, Currency.Usd);
        var apartment = ApartmentData.Create(price, cleaningFee);
        var pricingService = new PricingService();

        // Act
        var pricingDetails = pricingService.CalculatePrice(apartment, period);

        // Assert
        pricingDetails.TotalPrice.Should().Be(expectedTotalPrice);
    }

    [Fact]
    public void CalculatePrice_Should_ReturnZeroAmenitiesUpCharge_WhenApartmentHasNoAmenities()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var pricingService = new PricingService();
        var bookingTestPeriod = BookingData.Duration;

        // Act
        var pricingDetails = pricingService.CalculatePrice(apartment, bookingTestPeriod);

        // Assert
        pricingDetails.AmenitiesUpCharge.IsZero().Should().BeTrue();
    }

    [Theory]
    [InlineData(Amenity.GardenView)]
    [InlineData(Amenity.MountainView)]
    public void CalculatePrice_Should_Apply5PercentUpCharge_ForViewAmenities(Amenity amenity)
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.AddAmenity(amenity);
        var pricingService = new PricingService();
        var bookingTestPeriod = BookingData.Duration;

        // Act
        var pricingDetails = pricingService.CalculatePrice(apartment, bookingTestPeriod);

        // Assert
        var expected = new Money(pricingDetails.PriceForPeriod.Amount * 0.05m, Currency.Usd);
        pricingDetails.AmenitiesUpCharge.Should().Be(expected);
    }

    [Theory]
    [InlineData(Amenity.AirConditioning)]
    [InlineData(Amenity.Parking)]
    [InlineData(Amenity.WiFi)]
    [InlineData(Amenity.PetFriendly)]
    public void CalculatePrice_Should_Apply1PercentUpCharge_ForNonViewAmenities(Amenity amenity)
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.AddAmenity(amenity);
        var pricingService = new PricingService();
        var bookingTestPeriod = BookingData.Duration;

        // Act
        var pricingDetails = pricingService.CalculatePrice(apartment, bookingTestPeriod);

        // Assert
        var expected = new Money(pricingDetails.PriceForPeriod.Amount * 0.01m, Currency.Usd);
        pricingDetails.AmenitiesUpCharge.Should().Be(expected);
    }

    [Fact]
    public void CalculatePrice_Should_AccumulateUpCharges_AcrossMultipleAmenities()
    {
        // Arrange — GardenView (5%) + Wi-Fi (1%) = 6% combined, not just the
        // larger of the two. Confirms upcharges stack rather than the highest one winning.
        var apartment = ApartmentData.Create();
        apartment.AddAmenity(Amenity.GardenView);
        apartment.AddAmenity(Amenity.WiFi);
        var pricingService = new PricingService();
        var bookingTestPeriod = BookingData.Duration;

        // Act
        var pricingDetails = pricingService.CalculatePrice(apartment, bookingTestPeriod);

        // Assert
        var expected = new Money(pricingDetails.PriceForPeriod.Amount * 0.06m, Currency.Usd);
        pricingDetails.AmenitiesUpCharge.Should().Be(expected);
    }

    [Fact]
    public void CalculatePrice_Should_Throw_WhenCleaningFeeCurrencyDiffersFromPriceCurrency()
    {
        // Arrange
        var apartment = ApartmentData.Create(
            price: new Money(100m, Currency.Usd),
            cleaningFee: new Money(20m, Currency.Eur));
        var pricingService = new PricingService();
        var bookingTestPeriod = BookingData.Duration;

        // Act
        var act = () => pricingService.CalculatePrice(apartment, bookingTestPeriod);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CalculatePrice_Should_AccumulateUpCharges_AcrossMultipleViewAmenities()
    {
        var price = new Money(100m, Currency.Usd);
        var apartment = ApartmentData.Create(price);

        apartment.AddAmenity(Amenity.GardenView);
        apartment.AddAmenity(Amenity.MountainView);

        var period = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 11));

        var pricingService = new PricingService();

        var result = pricingService.CalculatePrice(apartment, period);

        result.AmenitiesUpCharge.Should()
            .Be(new Money(100m * 10 * 0.10m, Currency.Usd));
    }
}