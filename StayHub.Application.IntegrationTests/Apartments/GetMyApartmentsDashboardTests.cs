using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Apartments.GetMyApartmentsDashboard;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetMyApartmentsDashboardTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);
    private int _seedIndex;

    private DateOnly CurrentMonthStart => new(_today.Year, _today.Month, 1);
    private DateOnly PreviousMonthStart => CurrentMonthStart.AddMonths(-1);
    private DateOnly NextMonthStart => CurrentMonthStart.AddMonths(1);

    private static double ExpectedRate(int occupiedNights, int activeApartments, DateOnly monthStart) =>
        Math.Round(
            occupiedNights * 100d / (activeApartments * DateTime.DaysInMonth(monthStart.Year, monthStart.Month)),
            1);

    private async Task<(Guid OwnerId, List<Apartment> Apartments)> ArrangeOwnerAsync(
        int activeCount = 1, int inactiveCount = 0)
    {
        var owner = UserTestData.CreateUser();
        DbContext.Add(owner);

        var apartments = new List<Apartment>();

        for (var i = 0; i < activeCount; i++)
        {
            apartments.Add(ApartmentTestData.CreateApartment(ownerId: owner.Id, name: $"Active {i}"));
        }

        for (var i = 0; i < inactiveCount; i++)
        {
            var inactive = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: $"Inactive {i}");
            inactive.Deactivate();
            apartments.Add(inactive);
        }

        DbContext.AddRange(apartments);
        await DbContext.SaveChangesAsync();

        return (owner.Id, apartments);
    }

    private async Task SeedBookingAsync(
        Apartment apartment,
        BookingStatus status,
        DateOnly start,
        DateOnly end,
        decimal totalPrice = 100m,
        string currency = "USD")
    {
        var guest = UserTestData.CreateUser();
        DbContext.Add(guest);
        await DbContext.SaveChangesAsync();

        // Reserve through the domain at a unique far-future window so creation always succeeds...
        var farFuture = _today.AddYears(3).AddDays(_seedIndex++ * 10);
        var booking = BookingTestData.Reserve(apartment, guest.Id, farFuture, farFuture.AddDays(2), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        // ...then move it to the scenario under test directly in the database.
        await DbContext.Database.ExecuteSqlAsync($"""
                                                  UPDATE bookings
                                                  SET status = {(int)status},
                                                      duration_start = {start},
                                                      duration_end = {end},
                                                      total_price_amount = {totalPrice},
                                                      total_price_currency = {currency}
                                                  WHERE id = {booking.Id}
                                                  """);
    }

    [Fact]
    public async Task Dashboard_ShouldReturnZeros_WhenOwnerHasNoApartments()
    {
        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(Guid.CreateVersion7()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.ActiveCount.Should().Be(0);
        result.Value.InactiveCount.Should().Be(0);
        result.Value.PendingBookingsCount.Should().Be(0);
        result.Value.CurrentMonth.Should().Be(CurrentMonthStart);
        result.Value.CurrentMonthOccupancyRate.Should().Be(0);
        result.Value.PreviousMonthOccupancyRate.Should().Be(0);
        result.Value.MonthToDateRevenue.Should().BeEmpty();
    }

    [Fact]
    public async Task Dashboard_ShouldCountActiveAndInactiveApartments()
    {
        // Arrange
        var (ownerId, _) = await ArrangeOwnerAsync(activeCount: 2, inactiveCount: 1);

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(3);
        result.Value.ActiveCount.Should().Be(2);
        result.Value.InactiveCount.Should().Be(1);
    }

    [Fact]
    public async Task Dashboard_ShouldNotCountAnotherOwnersApartments()
    {
        // Arrange
        var (ownerId, _) = await ArrangeOwnerAsync(activeCount: 1);
        await ArrangeOwnerAsync(activeCount: 3, inactiveCount: 2);

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.ActiveCount.Should().Be(1);
    }

    [Fact]
    public async Task Dashboard_ShouldCountOnlyReservedBookingsStartingTodayOrLater_AsPending()
    {
        // Arrange
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 1);
        var apartment = apartments[0];

        await SeedBookingAsync(apartment, BookingStatus.Reserved, _today.AddDays(10), _today.AddDays(13)); // counts
        await SeedBookingAsync(apartment, BookingStatus.Reserved, _today, _today.AddDays(2)); // starts today: counts
        await SeedBookingAsync(apartment, BookingStatus.Reserved, _today.AddDays(-10), _today.AddDays(-5)); // stale
        await SeedBookingAsync(apartment, BookingStatus.Confirmed, _today.AddDays(20), _today.AddDays(23));
        await SeedBookingAsync(apartment, BookingStatus.Cancelled, _today.AddDays(30), _today.AddDays(33));

        var (_, otherApartments) = await ArrangeOwnerAsync(activeCount: 1);
        await SeedBookingAsync(otherApartments[0], BookingStatus.Reserved, _today.AddDays(10), _today.AddDays(13));

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PendingBookingsCount.Should().Be(2);
    }

    [Fact]
    public async Task Dashboard_ShouldCountPendingBookings_OnInactiveApartments_Too()
    {
        // Arrange
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 0, inactiveCount: 1);
        await SeedBookingAsync(apartments[0], BookingStatus.Reserved, _today.AddDays(10), _today.AddDays(13));

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PendingBookingsCount.Should().Be(1);
    }

    [Fact]
    public async Task Dashboard_ShouldCountConfirmedAndCompletedNights_ButNotReservedOrCancelled()
    {
        // Arrange
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 1);
        var apartment = apartments[0];
        var month = CurrentMonthStart;

        await SeedBookingAsync(apartment, BookingStatus.Confirmed, month, month.AddDays(10)); // 10 nights
        await SeedBookingAsync(apartment, BookingStatus.Completed, month.AddDays(10), month.AddDays(15)); // 5 nights
        await SeedBookingAsync(apartment, BookingStatus.Reserved, month.AddDays(15), month.AddDays(23)); // ignored
        await SeedBookingAsync(apartment, BookingStatus.Cancelled, month.AddDays(5), month.AddDays(13)); // ignored

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMonthOccupancyRate.Should().Be(ExpectedRate(15, 1, CurrentMonthStart));
        result.Value.PreviousMonthOccupancyRate.Should().Be(0);
    }

    [Fact]
    public async Task Dashboard_ShouldReturnPreviousMonthOccupancy_ForBookingsInThePreviousMonth()
    {
        // Arrange
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 1);
        await SeedBookingAsync(
            apartments[0], BookingStatus.Confirmed, PreviousMonthStart, PreviousMonthStart.AddDays(7)); // 7 nights

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PreviousMonthOccupancyRate.Should().Be(ExpectedRate(7, 1, PreviousMonthStart));
        result.Value.CurrentMonthOccupancyRate.Should().Be(0);
    }

    [Fact]
    public async Task Dashboard_ShouldSplitNightsAcrossMonths_WhenABookingSpansTheMonthBoundary()
    {
        // Arrange
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 1);
        await SeedBookingAsync(
            apartments[0],
            BookingStatus.Confirmed,
            CurrentMonthStart.AddDays(-3),
            CurrentMonthStart.AddDays(2));

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PreviousMonthOccupancyRate.Should().Be(ExpectedRate(3, 1, PreviousMonthStart));
        result.Value.CurrentMonthOccupancyRate.Should().Be(ExpectedRate(2, 1, CurrentMonthStart));
    }

    [Fact]
    public async Task Dashboard_ShouldIgnoreBookingsOutsideTheTwoMonths()
    {
        // Arrange
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 1);
        await SeedBookingAsync(
            apartments[0],
            BookingStatus.Confirmed,
            PreviousMonthStart.AddMonths(-1),
            PreviousMonthStart.AddMonths(-1).AddDays(10));
        await SeedBookingAsync(
            apartments[0],
            BookingStatus.Confirmed,
            NextMonthStart.AddDays(2),
            NextMonthStart.AddDays(9));

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMonthOccupancyRate.Should().Be(0);
        result.Value.PreviousMonthOccupancyRate.Should().Be(0);
    }

    [Fact]
    public async Task Dashboard_ShouldDivideByActiveApartments_WhenOwnerHasSeveral()
    {
        // Arrange
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 2);
        await SeedBookingAsync(
            apartments[0], BookingStatus.Confirmed, CurrentMonthStart, CurrentMonthStart.AddDays(10));

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMonthOccupancyRate.Should().Be(ExpectedRate(10, 2, CurrentMonthStart));
    }

    [Fact]
    public async Task Dashboard_ShouldExcludeInactiveApartments_FromOccupancy()
    {
        // Arrange 
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 1, inactiveCount: 1);
        var inactiveApartment = apartments[1];
        await SeedBookingAsync(
            inactiveApartment, BookingStatus.Confirmed, CurrentMonthStart, CurrentMonthStart.AddDays(10));

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMonthOccupancyRate.Should().Be(0);
    }

    [Fact]
    public async Task Dashboard_ShouldReturnZeroOccupancy_WhenOwnerHasNoActiveApartments()
    {
        // Arrange
        var (ownerId, _) = await ArrangeOwnerAsync(activeCount: 0, inactiveCount: 2);

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.CurrentMonthOccupancyRate.Should().Be(0);
        result.Value.PreviousMonthOccupancyRate.Should().Be(0);
    }

    [Fact]
    public async Task Dashboard_ShouldCapOccupancyAtOneHundred()
    {
        // Arrange
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 1);
        await SeedBookingAsync(apartments[0], BookingStatus.Confirmed, CurrentMonthStart, NextMonthStart);
        await SeedBookingAsync(apartments[0], BookingStatus.Confirmed, CurrentMonthStart, NextMonthStart);

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMonthOccupancyRate.Should().Be(100);
    }

    [Fact]
    public async Task Dashboard_ShouldSumRevenueByCurrency_ForConfirmedAndCompletedBookingsCheckingInThisMonth()
    {
        // Arrange
        var (ownerId, apartments) = await ArrangeOwnerAsync(activeCount: 1);
        var apartment = apartments[0];
        var month = CurrentMonthStart;

        // Counted
        await SeedBookingAsync(apartment, BookingStatus.Confirmed, month.AddDays(1), month.AddDays(4), 100m);
        // Check-in this month, stay runs into next month: still counted, by check-in date.
        await SeedBookingAsync(
            apartment, BookingStatus.Completed, month.AddDays(10), NextMonthStart.AddDays(2), 250m);
        await SeedBookingAsync(
            apartment, BookingStatus.Confirmed, month.AddDays(5), month.AddDays(8), 80m, currency: "EUR");

        // Excluded
        await SeedBookingAsync(apartment, BookingStatus.Reserved, month.AddDays(2), month.AddDays(5), 999m);
        await SeedBookingAsync(apartment, BookingStatus.Cancelled, month.AddDays(2), month.AddDays(5), 999m);
        await SeedBookingAsync(
            apartment, BookingStatus.Confirmed, PreviousMonthStart.AddDays(5), PreviousMonthStart.AddDays(8), 999m);
        await SeedBookingAsync(
            apartment, BookingStatus.Confirmed, NextMonthStart.AddDays(1), NextMonthStart.AddDays(4), 999m);

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MonthToDateRevenue.Should().BeEquivalentTo(new[]
        {
            new RevenueByCurrencyResponse("USD", 350m),
            new RevenueByCurrencyResponse("EUR", 80m)
        });
    }

    [Fact]
    public async Task Dashboard_ShouldNotIncludeAnotherOwnersRevenue()
    {
        // Arrange
        var (ownerId, _) = await ArrangeOwnerAsync(activeCount: 1);
        var (_, otherApartments) = await ArrangeOwnerAsync(activeCount: 1);
        await SeedBookingAsync(
            otherApartments[0], BookingStatus.Confirmed, CurrentMonthStart.AddDays(1), CurrentMonthStart.AddDays(4),
            500m);

        // Act
        var result = await Sender.Send(new GetMyApartmentsDashboardQuery(ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MonthToDateRevenue.Should().BeEmpty();
    }


    [Fact]
    public async Task Dashboard_ShouldServeSecondCallFromRealRedisCache_NotFromDatabase()
    {
        // Arrange
        var (ownerId, _) = await ArrangeOwnerAsync(activeCount: 1);
        var query = new GetMyApartmentsDashboardQuery(ownerId);

        // Act: first call misses, hits the database, then populates Redis.
        var first = await Sender.Send(query);
        first.IsSuccess.Should().BeTrue();
        first.Value.TotalCount.Should().Be(1);

        var cached = await CacheService.GetAsync<MyApartmentsDashboardResponse>(query.CacheKey);
        cached.Should().NotBeNull();

        // Change the underlying data directly.
        DbContext.Add(ApartmentTestData.CreateApartment(ownerId: ownerId, name: "Added after caching"));
        await DbContext.SaveChangesAsync();

        // Act: second call
        var second = await Sender.Send(query);

        // Assert — still the cached snapshot.
        second.IsSuccess.Should().BeTrue();
        second.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Dashboard_ShouldUseADifferentCacheKey_PerOwner()
    {
        // Arrange
        var (ownerA, _) = await ArrangeOwnerAsync(activeCount: 2);
        var (ownerB, _) = await ArrangeOwnerAsync(activeCount: 1);

        var queryA = new GetMyApartmentsDashboardQuery(ownerA);
        var queryB = new GetMyApartmentsDashboardQuery(ownerB);

        // Act
        var resultA = await Sender.Send(queryA);
        var resultB = await Sender.Send(queryB);

        // Assert
        queryA.CacheKey.Should().NotBe(queryB.CacheKey);
        resultA.Value.TotalCount.Should().Be(2);
        resultB.Value.TotalCount.Should().Be(1);
    }
}