using FluentAssertions;
using StayHub.Application.Apartments.GetApartmentPricing;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetApartmentPricingTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetApartmentPricing_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var query = new GetApartmentPricingQuery(
            Guid.CreateVersion7(), new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15));

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetApartmentPricing_ShouldReturnNotFound_WhenApartmentIsInactive()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        apartment.Deactivate();
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentPricingQuery(
            apartment.Id, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15));

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetApartmentPricing_ShouldReturnFailure_WhenStartIsNotBeforeEnd()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentPricingQuery(
            apartment.Id, new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 10));

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.InvalidDateRange);
    }

    [Fact]
    public async Task GetApartmentPricing_ShouldReturnCorrectBreakdown_WhenApartmentIsAvailable()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id, priceAmount: 100m, priceCurrency: "USD");
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentPricingQuery(
            apartment.Id, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15)); // 5 nights

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeTrue();
        result.Value.Nights.Should().Be(5);
        result.Value.PricePerNight.Should().Be(100m);
        result.Value.SubtotalForStay.Should().Be(500m);
        result.Value.Currency.Should().Be("USD");
        result.Value.TotalPrice.Should().Be(result.Value.SubtotalForStay
                                            + result.Value.CleaningFee
                                            + result.Value.AmenitiesUpcharge);
    }

    [Fact]
    public async Task GetApartmentPricing_ShouldReturnUnavailable_WhenConfirmedBookingOverlaps()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var booker = UserTestData.CreateUser();
        DbContext.AddRange(owner, apartment, booker);
        await DbContext.SaveChangesAsync();

        var duration = DateRange.Create(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15));
        var reserveResult = Booking.Reserve(apartment, booker.Id, duration, PricingService, DateTime.UtcNow);
        reserveResult.IsSuccess.Should().BeTrue();
        reserveResult.Value.Confirm(DateTime.UtcNow);

        DbContext.Add(reserveResult.Value);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentPricingQuery(
            apartment.Id, new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 18));

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeFalse();
        result.Value.Nights.Should().Be(0);
        result.Value.TotalPrice.Should().Be(0m);
    }

    [Fact]
    public async Task GetApartmentPricing_ShouldReturnAvailable_WhenOnlyReservedBookingOverlaps()
    {
        // Arrange — mirrors SearchApartments: a merely Reserved booking must not block availability.
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var booker = UserTestData.CreateUser();
        DbContext.AddRange(owner, apartment, booker);
        await DbContext.SaveChangesAsync();

        var duration = DateRange.Create(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15));
        var reserveResult = Booking.Reserve(apartment, booker.Id, duration, PricingService, DateTime.UtcNow);
        reserveResult.IsSuccess.Should().BeTrue();

        DbContext.Add(reserveResult.Value);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentPricingQuery(
            apartment.Id, new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 18));

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task GetApartmentPricing_ShouldReturnUnavailable_WhenAvailabilityBlockOverlaps()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var block = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 10),
            ApartmentUnavailabilityReason.UnderMaintenance,
            DateTime.UtcNow);

        DbContext.Add(block);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentPricingQuery(
            apartment.Id, new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 12));

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeFalse();
    }
}