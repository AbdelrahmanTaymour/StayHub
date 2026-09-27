using FluentAssertions;
using StayHub.Application.Apartments.GetApartmentsByOwner;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetApartmentsByOwnerTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetApartmentsByOwner_ShouldReturnEmptyPage_WhenOwnerHasNoApartments()
    {
        // Arrange
        var query = new GetApartmentsByOwnerQuery(Guid.CreateVersion7(), IncludeInactive: false, Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldReturnOnlyThatOwnersApartments_WithMappedPriceAndCountry()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var otherOwner = UserTestData.CreateUser();

        var ownedApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Owned Apartment",
            priceAmount: 300m,
            priceCurrency: "USD",
            city: "Cairo");

        var otherOwnersApartment = ApartmentTestData.CreateApartment(
            ownerId: otherOwner.Id,
            name: "Someone Else's Apartment");

        DbContext.AddRange(owner, otherOwner, ownedApartment, otherOwnersApartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(owner.Id, IncludeInactive: false, Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(ownedApartment.Id);
        result.Value.Items[0].PricePerNight.Should().Be(300m);
        result.Value.Items[0].Currency.Should().Be("USD");
        result.Value.Items[0].City.Should().Be("Cairo");
        result.Value.Items[0].Country.Should().Be("Egypt");
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldRespectPagination()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var baseTime = DateTime.UtcNow;

        var apartments = Enumerable.Range(0, 3)
            .Select(i => ApartmentTestData.CreateApartment(
                ownerId: owner.Id,
                name: $"Apartment {i}",
                utcNow: baseTime.AddMinutes(i)))
            .ToList();

        DbContext.Add(owner);
        DbContext.AddRange(apartments);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(
            owner.Id, IncludeInactive: false, Sort: OwnerApartmentsSort.PriceAsc, Page: 1, PageSize: 2);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be(apartments[2].Id);
        result.Value.Items[1].Id.Should().Be(apartments[1].Id);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldReturnRemainingItem_OnSecondPage()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var baseTime = DateTime.UtcNow;

        var apartments = Enumerable.Range(0, 3)
            .Select(i => ApartmentTestData.CreateApartment(
                ownerId: owner.Id,
                name: $"Apartment {i}",
                utcNow: baseTime.AddMinutes(i)))
            .ToList();

        DbContext.Add(owner);
        DbContext.AddRange(apartments);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(
            owner.Id, IncludeInactive: false, Sort: OwnerApartmentsSort.PriceAsc, Page: 2, PageSize: 2);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(a => a.Id == apartments[0].Id);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldExcludeInactiveApartments_ByDefault()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var activeApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Active");
        var inactiveApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Inactive");
        inactiveApartment.Deactivate();
        DbContext.AddRange(owner, activeApartment, inactiveApartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(owner.Id, IncludeInactive: false, Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Id.Should().Be(activeApartment.Id);
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldSortByPriceAscending_ByDefault()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var cheap = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Cheap", priceAmount: 100m);
        var expensive = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Expensive", priceAmount: 900m);

        DbContext.AddRange(owner, cheap, expensive);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(owner.Id, IncludeInactive: false, Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be(cheap.Id);
        result.Value.Items[1].Id.Should().Be(expensive.Id);
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldSortByPriceDescending_WhenRequested()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var cheap = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Cheap", priceAmount: 100m);
        var expensive = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Expensive", priceAmount: 900m);

        DbContext.AddRange(owner, cheap, expensive);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(
            owner.Id, IncludeInactive: false, Sort: OwnerApartmentsSort.PriceDesc, Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Id.Should().Be(expensive.Id);
        result.Value.Items[1].Id.Should().Be(cheap.Id);
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldServeSecondCallFromRealRedisCache_NotFromDatabase()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Cached Apartment");
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(owner.Id, IncludeInactive: false, Page: 1, PageSize: 10);

        // Act: First call; cache miss, hits the database, then populates Redis.
        var firstResult = await Sender.Send(query);
        firstResult.IsSuccess.Should().BeTrue();
        firstResult.Value.Items.Should().ContainSingle(a => a.Id == apartment.Id);

        // Act: Prove the value actually landed in real Redis
        var cachedValue = await CacheService.GetAsync<PagedResponse<OwnerApartmentsResponse>>(query.CacheKey);
        cachedValue.Should().NotBeNull();

        // Act: Remove the apartment directly from Postgres
        DbContext.Remove(apartment);
        await DbContext.SaveChangesAsync();

        // Act: Second call
        var secondResult = await Sender.Send(query);

        // Assert
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Items.Should().ContainSingle(a => a.Id == apartment.Id);
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldNotShareCacheEntry_AcrossDifferentSortOrders()
    {
        // Arrange — regression test for the cache key previously omitting Sort,
        // which would let one sort order's cached page leak into another's.
        var owner = UserTestData.CreateUser();
        var cheap = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Cheap", priceAmount: 100m);
        var expensive = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Expensive", priceAmount: 900m);
        DbContext.AddRange(owner, cheap, expensive);
        await DbContext.SaveChangesAsync();

        var ascQuery = new GetApartmentsByOwnerQuery(
            owner.Id, IncludeInactive: false, Sort: OwnerApartmentsSort.PriceAsc, Page: 1, PageSize: 10);
        var descQuery = new GetApartmentsByOwnerQuery(
            owner.Id, IncludeInactive: false, Sort: OwnerApartmentsSort.PriceDesc, Page: 1, PageSize: 10);

        // Act
        var ascResult = await Sender.Send(ascQuery);
        var descResult = await Sender.Send(descQuery);

        // Assert
        ascResult.Value.Items[0].Id.Should().Be(cheap.Id);
        descResult.Value.Items[0].Id.Should().Be(expensive.Id);
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldNotBeCacheable_WhenIncludeInactiveIsTrue()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var activeApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Active");
        var inactiveApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Inactive");
        inactiveApartment.Deactivate();
        DbContext.AddRange(owner, activeApartment, inactiveApartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(owner.Id, IncludeInactive: true, Page: 1, PageSize: 10);

        // Assert
        query.IsCacheable.Should().BeFalse();

        // Act
        await Sender.Send(query);

        // Assert
        var cachedValue = await CacheService.GetAsync<PagedResponse<OwnerApartmentsResponse>>(query.CacheKey);
        cachedValue.Should().BeNull();
    }
}