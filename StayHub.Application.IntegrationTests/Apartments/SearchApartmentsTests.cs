using FluentAssertions;
using StayHub.Application.Apartments.SearchApartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.IntegrationTests.Apartments;

public class SearchApartmentsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task SearchApartments_ShouldReturnEmptyList_WhenDateRangeIsInvalid()
    {
        // Arrange
        var query = new SearchApartmentsQuery(
            City: null,
            MinPrice: null,
            MaxPrice: 700M,
            Start: new DateOnly(2026, 1, 10),
            End: new DateOnly(2026, 1, 1),
            Page: 1,
            PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchApartments_ShouldExcludeInactiveApartments()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var activeApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Active Apartment");
        var inactiveApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Inactive Apartment");
        inactiveApartment.Deactivate();

        DbContext.AddRange(owner, activeApartment, inactiveApartment);
        await DbContext.SaveChangesAsync();

        var query = new SearchApartmentsQuery(
            City: null, MinPrice: null, MaxPrice: null, Start: null, End: null, Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Id == activeApartment.Id);
    }

    [Fact]
    public async Task SearchApartments_ShouldFilterByCity_CaseInsensitively()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var cairoApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Cairo Place", city: "Cairo");
        var alexApartment =
            ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Alex Place", city: "Alexandria");

        DbContext.AddRange(owner, cairoApartment, alexApartment);
        await DbContext.SaveChangesAsync();

        var query = new SearchApartmentsQuery(
            City: "cairo", MinPrice: null, MaxPrice: null, Start: null, End: null, Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Id == cairoApartment.Id);
    }

    [Fact]
    public async Task SearchApartments_ShouldFilterByPriceRange()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var cheapApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Cheap", priceAmount: 100m);
        var midApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Mid", priceAmount: 500m);
        var expensiveApartment =
            ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Expensive", priceAmount: 2000m);

        DbContext.AddRange(owner, cheapApartment, midApartment, expensiveApartment);
        await DbContext.SaveChangesAsync();

        var query = new SearchApartmentsQuery(
            City: null, MinPrice: 200m, MaxPrice: 1000m, Start: null, End: null, Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Id == midApartment.Id);
    }

    [Fact]
    public async Task SearchApartments_ShouldExcludeApartment_WhenActiveBookingOverlapsRequestedRange()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Booked Apartment");
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var booker = UserTestData.CreateUser();
        DbContext.Add(booker);
        await DbContext.SaveChangesAsync();

        var duration = DateRange.Create(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15));
        var reserveResult = Booking.Reserve(apartment, booker.Id, duration, PricingService, DateTime.UtcNow);
        reserveResult.IsSuccess.Should().BeTrue();

        DbContext.Add(reserveResult.Value);
        await DbContext.SaveChangesAsync();

        var query = new SearchApartmentsQuery(
            City: null,
            MinPrice: null,
            MaxPrice: null,
            Start: new DateOnly(2026, 6, 12),
            End: new DateOnly(2026, 6, 18),
            Page: 1,
            PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotContain(a => a.Id == apartment.Id);
    }

    [Fact]
    public async Task SearchApartments_ShouldExcludeApartment_WhenAvailabilityBlockOverlapsRequestedRange()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Blocked Apartment");
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

        var query = new SearchApartmentsQuery(
            City: null,
            MinPrice: null,
            MaxPrice: null,
            Start: new DateOnly(2026, 7, 5),
            End: new DateOnly(2026, 7, 12),
            Page: 1,
            PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotContain(a => a.Id == apartment.Id);
    }

    [Fact]
    public async Task SearchApartments_ShouldRespectPagination()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var baseTime = DateTime.UtcNow;

        var apartments = Enumerable.Range(0, 3)
            .Select(i =>
                ApartmentTestData.CreateApartment(ownerId: owner.Id, name: $"Paged {i}",
                    utcNow: baseTime.AddMinutes(i)))
            .ToList();

        DbContext.Add(owner);
        DbContext.AddRange(apartments);
        await DbContext.SaveChangesAsync();

        var query = new SearchApartmentsQuery(
            City: null, MinPrice: null, MaxPrice: null, Start: null, End: null, Page: 2, PageSize: 2);

        // Act
        var result = await Sender.Send(query);

        // Assert — page 2 with page size 2, 3 total rows ordered DESC by
        // created_on_utc, should return exactly the oldest one.
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Id == apartments[0].Id);
    }
}